using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.CustomerInvoice.Commands;

public sealed record DeleteCustomerInvoiceCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeleteCustomerInvoiceCommandHandler
    : IRequestHandler<DeleteCustomerInvoiceCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.CustomerInvoice, long> _repo;
    private readonly IRepository<Domain.Entities.SalesOrderLine, long> _soLineRepo;
    private readonly IUnitOfWork _uow;

    public DeleteCustomerInvoiceCommandHandler(
        IRepository<Domain.Entities.CustomerInvoice, long> repo,
        IRepository<Domain.Entities.SalesOrderLine, long> soLineRepo,
        IUnitOfWork uow)
    {
        _repo = repo;
        _soLineRepo = soLineRepo;
        _uow = uow;
    }

    public async Task<ApiResponse> Handle(DeleteCustomerInvoiceCommand cmd, CancellationToken cancellationToken)
    {
        var inv = await _repo.Query()
            .Include(c => c.Lines)
            .FirstOrDefaultAsync(c => c.Id == cmd.Id, cancellationToken);
        if (inv is null) return ApiResponse.Fail("Customer invoice not found.");

        if (inv.Status != Domain.Entities.CustomerInvoiceStatus.Draft &&
            inv.Status != Domain.Entities.CustomerInvoiceStatus.Cancelled)
        {
            return ApiResponse.Fail("Only draft or cancelled customer invoices can be deleted. Cancel it first.");
        }

        // A Draft still holds its SO-line coverage — release it on delete. A Cancelled invoice was
        // already released at cancel time, so don't double-release.
        if (inv.Status == Domain.Entities.CustomerInvoiceStatus.Draft)
        {
            await SalesOrderInvoiceCoverage.ReleaseAsync(
                _soLineRepo, inv.Lines.Select(l => (l.SalesOrderLineId, l.Quantity)), cancellationToken);
        }

        _repo.Remove(inv);
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Customer invoice deleted.");
    }
}
