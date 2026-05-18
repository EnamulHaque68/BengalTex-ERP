using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.DeliveryNote.Commands;

public sealed record DeleteDeliveryNoteCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeleteDeliveryNoteCommandHandler
    : IRequestHandler<DeleteDeliveryNoteCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.DeliveryNote, long> _repo;
    private readonly IUnitOfWork _uow;

    public DeleteDeliveryNoteCommandHandler(
        IRepository<Domain.Entities.DeliveryNote, long> repo,
        IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse> Handle(DeleteDeliveryNoteCommand cmd, CancellationToken cancellationToken)
    {
        var dn = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (dn is null) return ApiResponse.Fail("Delivery note not found.");

        // Posted DNs are immutable — they've already updated SO line DispatchedQuantity
        // and posted stock movements.
        if (dn.Status != Domain.Entities.DeliveryNoteStatus.Draft)
            return ApiResponse.Fail("Only draft delivery notes can be deleted.");

        _repo.Remove(dn);
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Delivery note deleted.");
    }
}
