using BengalTex.ERP.Application.Accounting;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Application.SupplierInvoice.Dtos;
using BengalTex.ERP.Application.SupplierInvoice.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;

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
    private readonly IUnitOfWork _uow;
    private readonly IJournalPostingService _journal;
    private readonly IMediator _mediator;

    public CancelSupplierInvoiceCommandHandler(
        IRepository<Domain.Entities.SupplierInvoice, long> repo,
        IUnitOfWork uow,
        IJournalPostingService journal,
        IMediator mediator)
    {
        _repo = repo;
        _uow = uow;
        _journal = journal;
        _mediator = mediator;
    }

    public async Task<ApiResponse<SupplierInvoiceDto>> Handle(
        CancelSupplierInvoiceCommand cmd, CancellationToken cancellationToken)
    {
        var inv = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
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

        // If it was Approved, an auto-journal exists — reverse it (Cr RM Inv / Cr VAT-in / Dr AP).
        if (wasApproved)
        {
            var rate = inv.ExchangeRate;
            await _journal.PostAsync(
                DateOnly.FromDateTime(DateTime.UtcNow), $"Reversal of supplier bill {inv.Code}", "SupplierInvoiceReversal", inv.Id, inv.Code,
                new[]
                {
                    new JournalPostingLine(LedgerAccounts.RawMaterialInventory, 0m, inv.SubtotalAmount * rate),
                    new JournalPostingLine(LedgerAccounts.VatReceivable, 0m, inv.VatAmount * rate),
                    new JournalPostingLine(LedgerAccounts.AccountsPayable, inv.TotalAmount * rate, 0m),
                }, cancellationToken);
        }

        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetSupplierInvoiceByIdQuery(inv.Id), cancellationToken);
    }
}
