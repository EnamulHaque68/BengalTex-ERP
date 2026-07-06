using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Application.SupplierInvoice.Dtos;
using BengalTex.ERP.Application.SupplierInvoice.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.SupplierInvoice.Commands;

/// <summary>
/// Cancels a supplier invoice. Allowed only from Draft or Approved and only when
/// <c>AmountPaid</c> is zero. Lifecycle: Draft | Approved → Cancelled.
/// </summary>
public sealed record CancelSupplierInvoiceCommand(long Id) : IRequest<ApiResponse<SupplierInvoiceDto>>;

internal sealed class CancelSupplierInvoiceCommandHandler
    : IRequestHandler<CancelSupplierInvoiceCommand, ApiResponse<SupplierInvoiceDto>>
{
    private readonly IRepository<Domain.Entities.SupplierInvoice, long> _repo;
    private readonly IRepository<Domain.Entities.PurchaseOrder, long> _poRepo;
    private readonly IRepository<Domain.Entities.GoodsReceiptNote, long> _grnRepo;
    private readonly IUnitOfWork _uow;
    private readonly IJournalPostingService _journal;
    private readonly IMediator _mediator;

    public CancelSupplierInvoiceCommandHandler(
        IRepository<Domain.Entities.SupplierInvoice, long> repo,
        IRepository<Domain.Entities.PurchaseOrder, long> poRepo,
        IRepository<Domain.Entities.GoodsReceiptNote, long> grnRepo,
        IUnitOfWork uow,
        IJournalPostingService journal,
        IMediator mediator)
    {
        _repo = repo;
        _poRepo = poRepo;
        _grnRepo = grnRepo;
        _uow = uow;
        _journal = journal;
        _mediator = mediator;
    }

    public async Task<ApiResponse<SupplierInvoiceDto>> Handle(
        CancelSupplierInvoiceCommand cmd, CancellationToken cancellationToken)
    {
        var inv = await _repo.Query()
            .Include(s => s.Lines).ThenInclude(l => l.Account)
            .FirstOrDefaultAsync(s => s.Id == cmd.Id, cancellationToken);
        if (inv is null) return ApiResponse<SupplierInvoiceDto>.Fail("Supplier invoice not found.");

        if (inv.Status != Domain.Entities.SupplierInvoiceStatus.Draft &&
            inv.Status != Domain.Entities.SupplierInvoiceStatus.Approved)
        {
            return ApiResponse<SupplierInvoiceDto>.Fail(
                "Supplier invoice can only be cancelled from Draft or Approved state.");
        }

        if (inv.AmountPaid > 0m)
        {
            return ApiResponse<SupplierInvoiceDto>.Fail(
                "Cannot cancel: payments have already been made. Delete the payments first.");
        }

        var wasApproved = inv.Status == Domain.Entities.SupplierInvoiceStatus.Approved;

        inv.Status = Domain.Entities.SupplierInvoiceStatus.Cancelled;
        _repo.Update(inv);

        // If it was Approved (and not an opening bill), reverse the exact legs that were posted —
        // recompute them with the same new-vs-legacy path selector, then mirror (Phase A2).
        if (wasApproved && !inv.IsOpening)
        {
            var po = await _poRepo.Query()
                .Include(p => p.Lines)
                .FirstOrDefaultAsync(p => p.Id == inv.PurchaseOrderId, cancellationToken);
            if (po is null) return ApiResponse<SupplierInvoiceDto>.Fail("Parent purchase order not found.");

            var useGrIrPath = await _grnRepo.Query().AnyAsync(
                g => g.PurchaseOrderId == po.Id && g.IsGlPosted, cancellationToken);

            var legs = SupplierBillPosting.Mirror(SupplierBillPosting.BuildApprovalLegs(inv, po, useGrIrPath));
            await _journal.PostAsync(
                DateOnly.FromDateTime(DateTime.UtcNow),
                $"Reversal of supplier bill {inv.Code}", "SupplierInvoiceReversal", inv.Id, inv.Code,
                legs, cancellationToken);
        }

        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetSupplierInvoiceByIdQuery(inv.Id), cancellationToken);
    }
}
