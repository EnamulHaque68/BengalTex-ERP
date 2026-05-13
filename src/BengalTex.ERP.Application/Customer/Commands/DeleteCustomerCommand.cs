using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.Customer.Commands;

public sealed record DeleteCustomerCommand(int Id) : IRequest<ApiResponse>;

internal sealed class DeleteCustomerCommandHandler : IRequestHandler<DeleteCustomerCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.Customer> _repo;
    private readonly IUnitOfWork _uow;

    public DeleteCustomerCommandHandler(IRepository<Domain.Entities.Customer> repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse> Handle(DeleteCustomerCommand cmd, CancellationToken cancellationToken)
    {
        var entity = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (entity is null) return ApiResponse.Fail("Customer not found.");

        // Note: once Sales Orders / Invoices reference customers, this should block delete
        // for customers with transactional history. For MVP we soft-delete unconditionally —
        // the AuditInterceptor preserves the row, just hides it from queries.
        _repo.Remove(entity);
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Customer deleted.");
    }
}
