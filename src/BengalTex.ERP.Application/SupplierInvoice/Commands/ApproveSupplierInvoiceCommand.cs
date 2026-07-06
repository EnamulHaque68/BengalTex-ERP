using BengalTex.ERP.Application.Accounting;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Application.SupplierInvoice.Dtos;
using BengalTex.ERP.Application.SupplierInvoice.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.SupplierInvoice.Commands;

/// <summary>
/// Approves a Draft supplier invoice — locks lines/total and authorizes it for
/// payment. Lifecycle: Draft → Approved. Once Approved, lines cannot be edited;
/// only AmountPaid/Status change via Payment create/delete. Mirror of
/// <c>IssueCustomerInvoiceCommand</c>; called "Approve" here because we're
/// internally authorizing the supplier's bill (we don't "issue" it).
/// </summary>
public sealed record ApproveSupplierInvoiceCommand(long Id) : IRequest<ApiResponse<SupplierInvoiceDto>>;

internal sealed class ApproveSupplierInvoiceCommandHandler
    : IRequestHandler<ApproveSupplierInvoiceCommand, ApiResponse<SupplierInvoiceDto>>
{
    private readonly IRepository<Domain.Entities.SupplierInvoice, long> _repo;
    private readonly IRepository<Domain.Entities.PurchaseOrder, long> _poRepo;
    private readonly IRepository<Domain.Entities.GoodsReceiptNote, long> _grnRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IJournalPostingService _journal;
    private readonly IMediator _mediator;

    public ApproveSupplierInvoiceCommandHandler(
        IRepository<Domain.Entities.SupplierInvoice, long> repo,
        IRepository<Domain.Entities.PurchaseOrder, long> poRepo,
        IRepository<Domain.Entities.GoodsReceiptNote, long> grnRepo,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        IJournalPostingService journal,
        IMediator mediator)
    {
        _repo = repo;
        _poRepo = poRepo;
        _grnRepo = grnRepo;
        _uow = uow;
        _currentUser = currentUser;
        _journal = journal;
        _mediator = mediator;
    }

    public async Task<ApiResponse<SupplierInvoiceDto>> Handle(
        ApproveSupplierInvoiceCommand cmd, CancellationToken cancellationToken)
    {
        var inv = await _repo.Query()
            .Include(s => s.Lines).ThenInclude(l => l.Account)
            .FirstOrDefaultAsync(s => s.Id == cmd.Id, cancellationToken);
        if (inv is null) return ApiResponse<SupplierInvoiceDto>.Fail("Supplier invoice not found.");
        if (inv.Status != Domain.Entities.SupplierInvoiceStatus.Draft)
            return ApiResponse<SupplierInvoiceDto>.Fail("Only draft supplier invoices can be approved.");
        if (inv.Lines.Count == 0)
            return ApiResponse<SupplierInvoiceDto>.Fail("Cannot approve an invoice with no lines.");

        // Recompute subtotal/VAT/total defensively (already kept in sync by Create/Update)
        inv.SubtotalAmount = inv.Lines.Sum(l => l.Quantity * l.UnitPrice);
        inv.VatAmount = Math.Round(inv.SubtotalAmount * inv.VatRate, 4, MidpointRounding.AwayFromZero);
        inv.TotalAmount = inv.SubtotalAmount + inv.VatAmount;

        // Phase A1: SUPPRESSED for opening bills — their GL value lives on the opening-balance
        // voucher; posting here would double-count AP. Payments/ageing still work.
        // (All validation + posting runs BEFORE mutating status, so a refusal leaves the bill Draft.)
        if (!inv.IsOpening)
        {
            var po = await _poRepo.Query()
                .Include(p => p.Lines)
                .FirstOrDefaultAsync(p => p.Id == inv.PurchaseOrderId, cancellationToken);
            if (po is null) return ApiResponse<SupplierInvoiceDto>.Fail("Parent purchase order not found.");

            // Phase A2 — new (GR/IR clearing) vs legacy (debit inventory) path: the new path applies
            // once any GRN on this PO has posted its receipt journal (or the GR/IR init ran).
            var useGrIrPath = await _grnRepo.Query().AnyAsync(
                g => g.PurchaseOrderId == po.Id && g.IsGlPosted, cancellationToken);

            if (useGrIrPath)
            {
                // Over-billing guard: cumulative approved bill qty per RM ≤ cumulative received qty.
                var guard = await CheckOverBillingAsync(inv, po, cancellationToken);
                if (guard is not null) return ApiResponse<SupplierInvoiceDto>.Fail(guard);
            }

            var legs = SupplierBillPosting.BuildApprovalLegs(inv, po, useGrIrPath);
            await _journal.PostAsync(
                inv.InvoiceDate, $"Supplier bill {inv.Code}", "SupplierInvoice", inv.Id, inv.Code,
                legs, cancellationToken);
        }

        inv.Status = Domain.Entities.SupplierInvoiceStatus.Approved;
        inv.ApprovedAt = DateTimeOffset.UtcNow;
        inv.ApprovedBy = _currentUser.UserName;

        _repo.Update(inv);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetSupplierInvoiceByIdQuery(inv.Id), cancellationToken);
    }

    /// <summary>
    /// Blocks approving a bill for more of a raw material than has been received (net of returns)
    /// across the PO. Returns null when OK, otherwise a human-readable refusal.
    /// </summary>
    private async Task<string?> CheckOverBillingAsync(
        Domain.Entities.SupplierInvoice inv, Domain.Entities.PurchaseOrder po, CancellationToken ct)
    {
        // This bill's material quantity per RM.
        var thisBill = inv.Lines
            .Where(l => l.RawMaterialId.HasValue)
            .GroupBy(l => l.RawMaterialId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));
        if (thisBill.Count == 0) return null;

        // Net received per RM = Σ PO-line ReceivedQuantity (already decremented by supplier returns).
        var receivedByRm = po.Lines
            .GroupBy(l => l.RawMaterialId)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.ReceivedQuantity));

        // Already-billed per RM = Σ material qty on other non-cancelled bills of this PO.
        var otherLines = await _repo.Query()
            .Where(s => s.PurchaseOrderId == po.Id && s.Id != inv.Id
                     && s.Status != Domain.Entities.SupplierInvoiceStatus.Cancelled)
            .SelectMany(s => s.Lines)
            .Where(l => l.RawMaterialId.HasValue)
            .GroupBy(l => l.RawMaterialId!.Value)
            .Select(g => new { Rm = g.Key, Qty = g.Sum(l => l.Quantity) })
            .ToListAsync(ct);
        var billedByRm = otherLines.ToDictionary(x => x.Rm, x => x.Qty);

        var rmNames = await _repo.Query()
            .Where(s => s.Id == inv.Id)
            .SelectMany(s => s.Lines)
            .Where(l => l.RawMaterialId.HasValue)
            .Select(l => new { Id = l.RawMaterialId!.Value, Name = l.RawMaterial!.Name })
            .Distinct()
            .ToDictionaryAsync(x => x.Id, x => x.Name, ct);

        foreach (var (rm, qty) in thisBill)
        {
            var received = receivedByRm.TryGetValue(rm, out var r) ? r : 0m;
            var billed = billedByRm.TryGetValue(rm, out var b) ? b : 0m;
            if (billed + qty > received + 0.0001m)
            {
                var name = rmNames.TryGetValue(rm, out var n) ? n : $"material {rm}";
                return $"{name}: billing {qty:0.####} would exceed received quantity " +
                       $"({received - billed:0.####} available to bill of {received:0.####} received).";
            }
        }
        return null;
    }
}
