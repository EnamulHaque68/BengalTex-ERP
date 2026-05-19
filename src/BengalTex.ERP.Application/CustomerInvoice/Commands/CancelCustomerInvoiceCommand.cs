using BengalTex.ERP.Application.CustomerInvoice.Dtos;
using BengalTex.ERP.Application.CustomerInvoice.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.CustomerInvoice.Commands;

/// <summary>
/// Cancels a customer invoice. Allowed only from Draft or Issued and only when
/// <c>AmountPaid</c> is zero — once receipts have been applied, cancellation would
/// orphan paid amounts. Lifecycle: Draft | Issued → Cancelled.
///
/// If an Issued invoice has an auto-generated <c>VatChallan</c>, the challan is
/// soft-deleted alongside the invoice (the legal document is no longer valid).
/// </summary>
public sealed record CancelCustomerInvoiceCommand(long Id) : IRequest<ApiResponse<CustomerInvoiceDto>>;

internal sealed class CancelCustomerInvoiceCommandHandler
    : IRequestHandler<CancelCustomerInvoiceCommand, ApiResponse<CustomerInvoiceDto>>
{
    private readonly IRepository<Domain.Entities.CustomerInvoice, long> _repo;
    private readonly IRepository<Domain.Entities.VatChallan, long> _challanRepo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public CancelCustomerInvoiceCommandHandler(
        IRepository<Domain.Entities.CustomerInvoice, long> repo,
        IRepository<Domain.Entities.VatChallan, long> challanRepo,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _repo = repo;
        _challanRepo = challanRepo;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<CustomerInvoiceDto>> Handle(
        CancelCustomerInvoiceCommand cmd, CancellationToken cancellationToken)
    {
        var inv = await _repo.Query()
            .Include(c => c.VatChallan)
            .FirstOrDefaultAsync(c => c.Id == cmd.Id, cancellationToken);
        if (inv is null) return ApiResponse<CustomerInvoiceDto>.Fail("Customer invoice not found.");

        if (inv.Status != Domain.Entities.CustomerInvoiceStatus.Draft &&
            inv.Status != Domain.Entities.CustomerInvoiceStatus.Issued)
        {
            return ApiResponse<CustomerInvoiceDto>.Fail(
                "Customer invoice can only be cancelled from Draft or Issued state.");
        }

        if (inv.AmountPaid > 0m)
        {
            return ApiResponse<CustomerInvoiceDto>.Fail(
                "Cannot cancel: receipts have already been applied. Delete the receipts first.");
        }

        if (inv.VatChallan is not null)
        {
            _challanRepo.Remove(inv.VatChallan);
        }

        inv.Status = Domain.Entities.CustomerInvoiceStatus.Cancelled;
        _repo.Update(inv);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetCustomerInvoiceByIdQuery(inv.Id), cancellationToken);
    }
}
