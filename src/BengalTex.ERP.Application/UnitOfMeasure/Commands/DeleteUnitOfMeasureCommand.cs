using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.UnitOfMeasure.Commands;

public sealed record DeleteUnitOfMeasureCommand(int Id) : IRequest<ApiResponse>;

internal sealed class DeleteUnitOfMeasureCommandHandler
    : IRequestHandler<DeleteUnitOfMeasureCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.UnitOfMeasure> _repo;
    private readonly IUnitOfWork _uow;

    public DeleteUnitOfMeasureCommandHandler(IRepository<Domain.Entities.UnitOfMeasure> repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse> Handle(DeleteUnitOfMeasureCommand cmd, CancellationToken cancellationToken)
    {
        var entity = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (entity is null) return ApiResponse.Fail("Unit of measure not found.");

        // Block delete if any other unit depends on this as its base — preserves the
        // conversion tree. Soft-delete via AuditInterceptor would still leave the FK,
        // but a hard pre-check is friendlier than a SQL constraint error.
        var hasDerivatives = await _repo.Query()
            .AnyAsync(u => u.BaseUnitId == cmd.Id, cancellationToken);
        if (hasDerivatives)
            return ApiResponse.Fail(
                $"Unit '{entity.Code}' is used as the base unit for one or more derivatives. Remove them first.");

        _repo.Remove(entity);
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Unit of measure deleted.");
    }
}
