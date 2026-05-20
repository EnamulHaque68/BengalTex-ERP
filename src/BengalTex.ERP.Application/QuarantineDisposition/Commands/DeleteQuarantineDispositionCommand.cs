using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.QuarantineDisposition.Commands;

/// <summary>Deletes a Draft disposition (soft delete). Posted dispositions are immutable.</summary>
public sealed record DeleteQuarantineDispositionCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeleteQuarantineDispositionCommandHandler
    : IRequestHandler<DeleteQuarantineDispositionCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.QuarantineDisposition, long> _repo;
    private readonly IUnitOfWork _uow;

    public DeleteQuarantineDispositionCommandHandler(
        IRepository<Domain.Entities.QuarantineDisposition, long> repo,
        IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse> Handle(DeleteQuarantineDispositionCommand cmd, CancellationToken cancellationToken)
    {
        var disp = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (disp is null) return ApiResponse.Fail("Quarantine disposition not found.");

        if (disp.Status != Domain.Entities.QuarantineDispositionStatus.Draft)
            return ApiResponse.Fail("Only draft dispositions can be deleted. Posted dispositions are immutable.");

        _repo.Remove(disp);
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Quarantine disposition deleted.");
    }
}
