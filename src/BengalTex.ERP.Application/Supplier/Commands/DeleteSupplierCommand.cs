using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.Supplier.Commands;

public sealed record DeleteSupplierCommand(int Id) : IRequest<ApiResponse>;

internal sealed class DeleteSupplierCommandHandler : IRequestHandler<DeleteSupplierCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.Supplier> _repo;
    private readonly IUnitOfWork _uow;

    public DeleteSupplierCommandHandler(IRepository<Domain.Entities.Supplier> repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse> Handle(DeleteSupplierCommand cmd, CancellationToken cancellationToken)
    {
        var entity = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (entity is null) return ApiResponse.Fail("Supplier not found.");

        // Future: block deletion when Purchase Orders / GRNs reference this supplier.
        _repo.Remove(entity);
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Supplier deleted.");
    }
}
