using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.SupplierInvoice.Commands;

public sealed record DeleteSupplierInvoiceCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeleteSupplierInvoiceCommandHandler
    : IRequestHandler<DeleteSupplierInvoiceCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.SupplierInvoice, long> _repo;
    private readonly IUnitOfWork _uow;

    public DeleteSupplierInvoiceCommandHandler(
        IRepository<Domain.Entities.SupplierInvoice, long> repo,
        IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse> Handle(DeleteSupplierInvoiceCommand cmd, CancellationToken cancellationToken)
    {
        var inv = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (inv is null) return ApiResponse.Fail("Supplier invoice not found.");

        if (inv.Status != Domain.Entities.SupplierInvoiceStatus.Draft &&
            inv.Status != Domain.Entities.SupplierInvoiceStatus.Cancelled)
        {
            return ApiResponse.Fail("Only draft or cancelled supplier invoices can be deleted. Cancel it first.");
        }

        _repo.Remove(inv);
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Supplier invoice deleted.");
    }
}
