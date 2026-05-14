using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.RawMaterial.Commands;

public sealed record DeleteRawMaterialCommand(int Id) : IRequest<ApiResponse>;

internal sealed class DeleteRawMaterialCommandHandler : IRequestHandler<DeleteRawMaterialCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.RawMaterial> _repo;
    private readonly IUnitOfWork _uow;

    public DeleteRawMaterialCommandHandler(IRepository<Domain.Entities.RawMaterial> repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse> Handle(DeleteRawMaterialCommand cmd, CancellationToken cancellationToken)
    {
        var entity = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (entity is null) return ApiResponse.Fail("Raw material not found.");

        // Future: block deletion when BOM lines / stock records / GRNs reference this material.
        _repo.Remove(entity);
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Raw material deleted.");
    }
}
