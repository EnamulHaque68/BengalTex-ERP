using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Accounting.InventoryGL;

// ═══════════════════════════ DTOs ═══════════════════════════

public sealed record TieOutRowDto(
    string Label, string AccountCode, decimal StockValue, decimal GlBalance, decimal Variance, bool Matches);

public sealed record OpenGrIrPoRowDto(
    long PurchaseOrderId, string PurchaseOrderCode, string SupplierName, decimal UnbilledValue);

public sealed record InventoryGlTieOutDto(
    DateOnly AsOfDate,
    IReadOnlyList<TieOutRowDto> Rows,
    decimal GrIrBalance,
    IReadOnlyList<OpenGrIrPoRowDto> OpenGrIr);

// ═══════════════════════════ Query ═══════════════════════════

/// <summary>
/// Phase A2 (D7) — reconciles perpetual stock valuation (Σ qty × WAC) against the GL inventory
/// accounts (RM 1140, FG 1150, WIP 1160) as of a date, and lists the PO-wise received-not-billed
/// schedule that makes up the GR/IR (2150) balance. The monthly audit tie-out.
/// </summary>
public sealed record GetInventoryGlTieOutQuery(DateOnly? AsOfDate = null)
    : IRequest<ApiResponse<InventoryGlTieOutDto>>;

internal sealed class GetInventoryGlTieOutQueryHandler
    : IRequestHandler<GetInventoryGlTieOutQuery, ApiResponse<InventoryGlTieOutDto>>
{
    private readonly IRepository<StockOnHand> _stockRepo;
    private readonly IRepository<Domain.Entities.RawMaterial> _rmRepo;
    private readonly IRepository<Domain.Entities.Product> _productRepo;
    private readonly IRepository<JournalEntryLine, long> _lineRepo;
    private readonly IRepository<Domain.Entities.PurchaseOrder, long> _poRepo;
    private readonly IRepository<Domain.Entities.GoodsReceiptNote, long> _grnRepo;
    private readonly IRepository<Domain.Entities.SupplierInvoice, long> _invRepo;

    public GetInventoryGlTieOutQueryHandler(
        IRepository<StockOnHand> stockRepo,
        IRepository<Domain.Entities.RawMaterial> rmRepo,
        IRepository<Domain.Entities.Product> productRepo,
        IRepository<JournalEntryLine, long> lineRepo,
        IRepository<Domain.Entities.PurchaseOrder, long> poRepo,
        IRepository<Domain.Entities.GoodsReceiptNote, long> grnRepo,
        IRepository<Domain.Entities.SupplierInvoice, long> invRepo)
    {
        _stockRepo = stockRepo; _rmRepo = rmRepo; _productRepo = productRepo;
        _lineRepo = lineRepo; _poRepo = poRepo; _grnRepo = grnRepo; _invRepo = invRepo;
    }

    public async Task<ApiResponse<InventoryGlTieOutDto>> Handle(
        GetInventoryGlTieOutQuery request, CancellationToken ct)
    {
        var asOf = request.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        // ── Perpetual stock valuation: Σ qty × current WAC ──
        var rmWac = await _rmRepo.Query().AsNoTracking()
            .Select(r => new { r.Id, r.WeightedAverageCost }).ToDictionaryAsync(x => x.Id, x => x.WeightedAverageCost, ct);
        var productWac = await _productRepo.Query().AsNoTracking()
            .Select(p => new { p.Id, p.WeightedAverageCost }).ToDictionaryAsync(x => x.Id, x => x.WeightedAverageCost, ct);

        var stock = await _stockRepo.Query().AsNoTracking()
            .Select(s => new { s.RawMaterialId, s.ProductId, s.Quantity }).ToListAsync(ct);

        decimal rmStockValue = 0m, fgStockValue = 0m;
        foreach (var s in stock)
        {
            if (s.RawMaterialId is int rm && rmWac.TryGetValue(rm, out var w)) rmStockValue += s.Quantity * w;
            else if (s.ProductId is int pid && productWac.TryGetValue(pid, out var pw)) fgStockValue += s.Quantity * pw;
        }

        // ── GL balances for the inventory + GR/IR accounts (posted lines up to the date) ──
        var codes = new[]
        {
            LedgerAccounts.RawMaterialInventory, LedgerAccounts.FinishedGoodsInventory,
            LedgerAccounts.WorkInProgressInventory, LedgerAccounts.GrIrClearing
        };
        var glByCode = await _lineRepo.Query().AsNoTracking()
            .Where(l => l.JournalEntry.Status == JournalEntryStatus.Posted
                     && l.JournalEntry.EntryDate <= asOf
                     && codes.Contains(l.Account.Code))
            .GroupBy(l => l.Account.Code)
            .Select(g => new { Code = g.Key, Balance = g.Sum(x => x.Debit) - g.Sum(x => x.Credit) })
            .ToDictionaryAsync(x => x.Code, x => x.Balance, ct);

        decimal Gl(string code) => glByCode.TryGetValue(code, out var b) ? b : 0m;

        static TieOutRowDto Row(string label, string code, decimal stockVal, decimal gl)
        {
            var variance = Math.Round(stockVal - gl, 2);
            return new TieOutRowDto(label, code, Math.Round(stockVal, 2), Math.Round(gl, 2), variance, Math.Abs(variance) < 0.01m);
        }

        var rows = new List<TieOutRowDto>
        {
            Row("Raw Material Inventory", LedgerAccounts.RawMaterialInventory, rmStockValue, Gl(LedgerAccounts.RawMaterialInventory)),
            Row("Finished Goods Inventory", LedgerAccounts.FinishedGoodsInventory, fgStockValue, Gl(LedgerAccounts.FinishedGoodsInventory)),
            // WIP has no perpetual stock snapshot until Phase A4 — GL shown, stock side 0 for now.
            Row("Work In Progress", LedgerAccounts.WorkInProgressInventory, 0m, Gl(LedgerAccounts.WorkInProgressInventory)),
        };

        // ── Open GR/IR schedule: PO-wise received-not-billed (reuses the init calc) ──
        var grIrRows = await GrIrInitCalc.ComputeAsync(_poRepo, _grnRepo, _invRepo, ct);
        var openGrIr = grIrRows.Where(r => r.Unbilled > 0m)
            .Select(r => new OpenGrIrPoRowDto(r.PoId, r.PoCode, r.Supplier, r.Unbilled))
            .ToList();

        return ApiResponse<InventoryGlTieOutDto>.Ok(new InventoryGlTieOutDto(
            asOf, rows, Math.Round(-Gl(LedgerAccounts.GrIrClearing), 2), openGrIr));
    }
}
