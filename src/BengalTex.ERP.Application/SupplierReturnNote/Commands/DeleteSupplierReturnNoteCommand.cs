using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.SupplierReturnNote.Commands;

public sealed record DeleteSupplierReturnNoteCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeleteSupplierReturnNoteCommandHandler
    : IRequestHandler<DeleteSupplierReturnNoteCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.SupplierReturnNote, long> _repo;
    private readonly IUnitOfWork _uow;

    public DeleteSupplierReturnNoteCommandHandler(
        IRepository<Domain.Entities.SupplierReturnNote, long> repo,
        IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse> Handle(DeleteSupplierReturnNoteCommand cmd, CancellationToken cancellationToken)
    {
        var srn = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (srn is null) return ApiResponse.Fail("Supplier return note not found.");

        if (srn.Status != Domain.Entities.SupplierReturnNoteStatus.Draft)
            return ApiResponse.Fail("Only draft supplier return notes can be deleted. Posted returns are immutable — post a counter-SRN to reverse.");

        _repo.Remove(srn);
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Supplier return note deleted.");
    }
}
