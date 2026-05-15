using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.Bom.Commands;

public sealed record DeleteBomCommand(int Id) : IRequest<ApiResponse>;

internal sealed class DeleteBomCommandHandler : IRequestHandler<DeleteBomCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.Bom> _repo;
    private readonly IUnitOfWork _uow;

    public DeleteBomCommandHandler(IRepository<Domain.Entities.Bom> repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse> Handle(DeleteBomCommand cmd, CancellationToken cancellationToken)
    {
        var bom = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (bom is null) return ApiResponse.Fail("BOM not found.");

        if (bom.IsActive)
            return ApiResponse.Fail("Cannot delete the active BOM. Activate another version first.");

        // Soft delete — child lines stay with the (now hidden) parent.
        _repo.Remove(bom);
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("BOM deleted.");
    }
}
