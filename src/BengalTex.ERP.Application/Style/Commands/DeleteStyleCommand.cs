using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.Style.Commands;

public sealed record DeleteStyleCommand(int Id) : IRequest<ApiResponse>;

internal sealed class DeleteStyleCommandHandler
    : IRequestHandler<DeleteStyleCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.Style> _repo;
    private readonly IUnitOfWork _uow;

    public DeleteStyleCommandHandler(IRepository<Domain.Entities.Style> repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse> Handle(DeleteStyleCommand cmd, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(cmd.Id, ct);
        if (entity is null) return ApiResponse.Fail("Style not found.");

        _repo.Remove(entity);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Style deleted.");
    }
}
