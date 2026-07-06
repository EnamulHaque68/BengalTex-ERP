using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Accounting.InventoryGL;

// ═══════════════════════════ DTOs ═══════════════════════════

public sealed record GrIrInitPoRowDto(
    long PurchaseOrderId, string PurchaseOrderCode, string SupplierName,
    decimal ReceivedValue, decimal BilledValue, decimal UnbilledValue);

public sealed record GrIrInitPreviewDto(
    bool AlreadyInitialized,
    decimal TotalUnbilledValue,
    IReadOnlyList<GrIrInitPoRowDto> Rows);

// ═══════════════════════════ Shared calc ═══════════════════════════

internal static class GrIrInitCalc
{
    public sealed record PoUnbilled(long PoId, string PoCode, string Supplier,
        decimal Received, decimal Billed, decimal Unbilled);

    /// <summary>
    /// For every PO carrying a pre-A2 (non-GL-posted) posted GRN, computes the received-not-billed
    /// value that should sit in GR/IR. Per RM: unbilled qty = max(0, net received − already billed);
    /// value = unbilled qty × PO price × PO rate. Materialize-then-compute (avoids nested SQL aggregates).
    /// </summary>
    public static async Task<List<PoUnbilled>> ComputeAsync(
        IRepository<Domain.Entities.PurchaseOrder, long> poRepo,
        IRepository<Domain.Entities.GoodsReceiptNote, long> grnRepo,
        IRepository<Domain.Entities.SupplierInvoice, long> invRepo,
        CancellationToken ct)
    {
        // POs that have at least one posted, not-yet-GL-posted GRN.
        var candidatePoIds = await grnRepo.Query().AsNoTracking()
            .Where(g => g.Status == GoodsReceiptStatus.Posted && !g.IsGlPosted)
            .Select(g => g.PurchaseOrderId)
            .Distinct()
            .ToListAsync(ct);
        if (candidatePoIds.Count == 0) return new List<PoUnbilled>();

        var pos = await poRepo.Query().AsNoTracking()
            .Where(p => candidatePoIds.Contains(p.Id))
            .Include(p => p.Lines)
            .Include(p => p.Supplier)
            .ToListAsync(ct);

        // Billed material qty per (PO, RM) across non-cancelled bills.
        var billedRows = await invRepo.Query().AsNoTracking()
            .Where(s => candidatePoIds.Contains(s.PurchaseOrderId)
                     && s.Status != SupplierInvoiceStatus.Cancelled)
            .SelectMany(s => s.Lines.Where(l => l.RawMaterialId != null)
                .Select(l => new { s.PurchaseOrderId, RmId = l.RawMaterialId!.Value, l.Quantity }))
            .ToListAsync(ct);

        var billedByPoRm = billedRows
            .GroupBy(x => (x.PurchaseOrderId, x.RmId))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

        var result = new List<PoUnbilled>();
        foreach (var po in pos)
        {
            decimal received = 0m, billed = 0m, unbilled = 0m;
            foreach (var line in po.Lines)
            {
                var poValue = line.ReceivedQuantity * line.UnitPrice * po.ExchangeRate;
                received += poValue;

                var billedQty = billedByPoRm.TryGetValue((po.Id, line.RawMaterialId), out var b) ? b : 0m;
                var billedValue = Math.Min(billedQty, line.ReceivedQuantity) * line.UnitPrice * po.ExchangeRate;
                billed += billedValue;

                var unbilledQty = line.ReceivedQuantity - billedQty;
                if (unbilledQty > 0m) unbilled += unbilledQty * line.UnitPrice * po.ExchangeRate;
            }
            if (received > 0m)
                result.Add(new PoUnbilled(po.Id, po.Code, po.Supplier.Name,
                    Math.Round(received, 2), Math.Round(billed, 2), Math.Round(unbilled, 2)));
        }
        return result.OrderBy(r => r.PoCode).ToList();
    }
}

// ═══════════════════════════ Preview ═══════════════════════════

public sealed record GetGrIrInitPreviewQuery : IRequest<ApiResponse<GrIrInitPreviewDto>>;

internal sealed class GetGrIrInitPreviewQueryHandler
    : IRequestHandler<GetGrIrInitPreviewQuery, ApiResponse<GrIrInitPreviewDto>>
{
    private readonly IRepository<Domain.Entities.PurchaseOrder, long> _poRepo;
    private readonly IRepository<Domain.Entities.GoodsReceiptNote, long> _grnRepo;
    private readonly IRepository<Domain.Entities.SupplierInvoice, long> _invRepo;
    private readonly IRepository<JournalEntry, long> _journalRepo;

    public GetGrIrInitPreviewQueryHandler(
        IRepository<Domain.Entities.PurchaseOrder, long> poRepo,
        IRepository<Domain.Entities.GoodsReceiptNote, long> grnRepo,
        IRepository<Domain.Entities.SupplierInvoice, long> invRepo,
        IRepository<JournalEntry, long> journalRepo)
    {
        _poRepo = poRepo; _grnRepo = grnRepo; _invRepo = invRepo; _journalRepo = journalRepo;
    }

    public async Task<ApiResponse<GrIrInitPreviewDto>> Handle(GetGrIrInitPreviewQuery q, CancellationToken ct)
    {
        var alreadyRun = await _journalRepo.Query().AnyAsync(
            j => j.SourceType == "GrIrInit" && j.Status == JournalEntryStatus.Posted, ct);

        var rows = await GrIrInitCalc.ComputeAsync(_poRepo, _grnRepo, _invRepo, ct);
        var dtoRows = rows.Where(r => r.Unbilled > 0m)
            .Select(r => new GrIrInitPoRowDto(r.PoId, r.PoCode, r.Supplier, r.Received, r.Billed, r.Unbilled))
            .ToList();

        return ApiResponse<GrIrInitPreviewDto>.Ok(new GrIrInitPreviewDto(
            alreadyRun, dtoRows.Sum(r => r.UnbilledValue), dtoRows));
    }
}

// ═══════════════════════════ Initialize ═══════════════════════════

/// <summary>
/// One-time catch-up (Phase A2): posts Dr RM Inventory / Cr GR/IR for the received-not-billed
/// value of all pre-A2 GRNs, bringing GL inventory in line with physical stock and establishing
/// the receipt liability, then marks those GRNs GL-posted so future bills clear GR/IR uniformly.
/// </summary>
public sealed record InitializeGrIrCommand(DateOnly AsOfDate) : IRequest<ApiResponse<long>>;

internal sealed class InitializeGrIrCommandHandler : IRequestHandler<InitializeGrIrCommand, ApiResponse<long>>
{
    private readonly IRepository<Domain.Entities.PurchaseOrder, long> _poRepo;
    private readonly IRepository<Domain.Entities.GoodsReceiptNote, long> _grnRepo;
    private readonly IRepository<Domain.Entities.SupplierInvoice, long> _invRepo;
    private readonly IRepository<JournalEntry, long> _journalRepo;
    private readonly IRepository<Domain.Entities.Account> _accountRepo;
    private readonly IPeriodGuard _periodGuard;
    private readonly INumberingService _numbering;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public InitializeGrIrCommandHandler(
        IRepository<Domain.Entities.PurchaseOrder, long> poRepo,
        IRepository<Domain.Entities.GoodsReceiptNote, long> grnRepo,
        IRepository<Domain.Entities.SupplierInvoice, long> invRepo,
        IRepository<JournalEntry, long> journalRepo,
        IRepository<Domain.Entities.Account> accountRepo,
        IPeriodGuard periodGuard,
        INumberingService numbering,
        ICurrentUserService currentUser,
        IUnitOfWork uow)
    {
        _poRepo = poRepo; _grnRepo = grnRepo; _invRepo = invRepo; _journalRepo = journalRepo;
        _accountRepo = accountRepo;
        _periodGuard = periodGuard; _numbering = numbering; _currentUser = currentUser; _uow = uow;
    }

    public async Task<ApiResponse<long>> Handle(InitializeGrIrCommand cmd, CancellationToken ct)
    {
        var alreadyRun = await _journalRepo.Query().AnyAsync(
            j => j.SourceType == "GrIrInit" && j.Status == JournalEntryStatus.Posted, ct);
        if (alreadyRun)
            return ApiResponse<long>.Fail("GR/IR has already been initialized. Reverse that voucher first to re-run.");

        var refusal = await _periodGuard.CheckAsync(cmd.AsOfDate, isManualVoucher: true, ct);
        if (refusal is not null) return ApiResponse<long>.Fail(refusal);

        var rows = await GrIrInitCalc.ComputeAsync(_poRepo, _grnRepo, _invRepo, ct);
        var totalUnbilled = Math.Round(rows.Sum(r => r.Unbilled), 2);

        // Mark every candidate GRN as GL-posted regardless of unbilled value — from now on all bills
        // on these POs follow the new (clear GR/IR) path uniformly.
        var candidateGrns = await _grnRepo.Query()
            .Where(g => g.Status == GoodsReceiptStatus.Posted && !g.IsGlPosted)
            .ToListAsync(ct);
        foreach (var g in candidateGrns) { g.IsGlPosted = true; _grnRepo.Update(g); }

        long entryId = 0;
        if (totalUnbilled > 0m)
        {
            var inventory = await _accountRepo.Query()
                .FirstOrDefaultAsync(a => a.Code == LedgerAccounts.RawMaterialInventory, ct);
            var grIr = await _accountRepo.Query()
                .FirstOrDefaultAsync(a => a.Code == LedgerAccounts.GrIrClearing, ct);
            if (inventory is null || grIr is null)
                return ApiResponse<long>.Fail("Inventory (1140) or GR/IR (2150) account not found in the chart of accounts.");

            var entry = new JournalEntry
            {
                Code = await _numbering.NextAsync("JV", null, ct),
                EntryDate = cmd.AsOfDate,
                Narration = "GR/IR initialization — received-not-billed inventory brought onto the ledger",
                Status = JournalEntryStatus.Posted,
                VoucherType = VoucherType.Journal,
                AccountingPeriodId = await _periodGuard.GetPeriodIdAsync(cmd.AsOfDate, ct),
                SourceType = "GrIrInit",
                SourceId = 0,
                SourceCode = "GRIR-INIT",
                PostedAt = DateTimeOffset.UtcNow,
                PostedBy = _currentUser.UserName ?? "system",
                Lines =
                {
                    new JournalEntryLine { AccountId = inventory.Id, Debit = totalUnbilled, Credit = 0m, SortOrder = 0 },
                    new JournalEntryLine { AccountId = grIr.Id, Debit = 0m, Credit = totalUnbilled, SortOrder = 1 }
                }
            };
            await _journalRepo.AddAsync(entry, ct);
            await _uow.SaveChangesAsync(ct);
            entryId = entry.Id;
        }
        else
        {
            await _uow.SaveChangesAsync(ct);   // still persist the IsGlPosted flags
        }

        return ApiResponse<long>.Ok(entryId,
            totalUnbilled > 0m
                ? $"GR/IR initialized — {totalUnbilled:N2} of received-not-billed inventory brought onto the ledger."
                : "GR/IR initialized — no unbilled receipts to catch up; existing receipts marked GL-posted.");
    }
}
