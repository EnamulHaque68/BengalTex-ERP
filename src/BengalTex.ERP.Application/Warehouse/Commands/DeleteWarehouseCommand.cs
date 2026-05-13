using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.Warehouse.Commands;

public sealed record DeleteWarehouseCommand(int Id) : IRequest<ApiResponse>;

internal sealed class DeleteWarehouseCommandHandler : IRequestHandler<DeleteWarehouseCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.Warehouse> _repo;
    private readonly IUnitOfWork _uow;

    public DeleteWarehouseCommandHandler(IRepository<Domain.Entities.Warehouse> repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse> Handle(DeleteWarehouseCommand cmd, CancellationToken cancellationToken)
    {
        var entity = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (entity is null) return ApiResponse.Fail("Warehouse not found.");

        _repo.Remove(entity);
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Warehouse deleted.");
    }
}
