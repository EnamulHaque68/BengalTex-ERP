using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.QcInspection.Commands;

/// <summary>
/// Deletes a Draft QC inspection (soft delete). Posted inspections are immutable —
/// rejected stock has already moved to quarantine; correct via inventory adjustment.
/// </summary>
public sealed record DeleteQcInspectionCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeleteQcInspectionCommandHandler
    : IRequestHandler<DeleteQcInspectionCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.QcInspection, long> _repo;
    private readonly IUnitOfWork _uow;

    public DeleteQcInspectionCommandHandler(
        IRepository<Domain.Entities.QcInspection, long> repo,
        IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse> Handle(DeleteQcInspectionCommand cmd, CancellationToken cancellationToken)
    {
        var insp = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (insp is null) return ApiResponse.Fail("QC inspection not found.");

        if (insp.Status != Domain.Entities.QcInspectionStatus.Draft)
            return ApiResponse.Fail("Only draft QC inspections can be deleted. Posted inspections are immutable.");

        _repo.Remove(insp);
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("QC inspection deleted.");
    }
}
