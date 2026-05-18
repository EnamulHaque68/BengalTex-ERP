using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.CustomerInvoice.Commands;

public sealed record DeleteCustomerInvoiceCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeleteCustomerInvoiceCommandHandler
    : IRequestHandler<DeleteCustomerInvoiceCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.CustomerInvoice, long> _repo;
    private readonly IUnitOfWork _uow;

    public DeleteCustomerInvoiceCommandHandler(
        IRepository<Domain.Entities.CustomerInvoice, long> repo,
        IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse> Handle(DeleteCustomerInvoiceCommand cmd, CancellationToken cancellationToken)
    {
        var inv = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (inv is null) return ApiResponse.Fail("Customer invoice not found.");

        if (inv.Status != Domain.Entities.CustomerInvoiceStatus.Draft &&
            inv.Status != Domain.Entities.CustomerInvoiceStatus.Cancelled)
        {
            return ApiResponse.Fail("Only draft or cancelled customer invoices can be deleted. Cancel it first.");
        }

        _repo.Remove(inv);
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Customer invoice deleted.");
    }
}
