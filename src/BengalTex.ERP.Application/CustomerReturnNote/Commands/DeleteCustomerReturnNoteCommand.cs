using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.CustomerReturnNote.Commands;

/// <summary>
/// Deletes a Draft customer return note (soft delete). Posted CRNs are immutable —
/// to reverse a posted return, post a counter-CRN.
/// </summary>
public sealed record DeleteCustomerReturnNoteCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeleteCustomerReturnNoteCommandHandler
    : IRequestHandler<DeleteCustomerReturnNoteCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.CustomerReturnNote, long> _repo;
    private readonly IUnitOfWork _uow;

    public DeleteCustomerReturnNoteCommandHandler(
        IRepository<Domain.Entities.CustomerReturnNote, long> repo,
        IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse> Handle(DeleteCustomerReturnNoteCommand cmd, CancellationToken cancellationToken)
    {
        var crn = await _repo.GetByIdAsync(cmd.Id, cancellationToken);
        if (crn is null) return ApiResponse.Fail("Customer return note not found.");

        if (crn.Status != Domain.Entities.CustomerReturnNoteStatus.Draft)
            return ApiResponse.Fail("Only draft customer return notes can be deleted. Posted returns are immutable — post a counter-CRN to reverse.");

        _repo.Remove(crn);
        await _uow.SaveChangesAsync(cancellationToken);

        return ApiResponse.Ok("Customer return note deleted.");
    }
}
