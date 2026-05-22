using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.Banking.Commands;

public sealed record DeleteLetterOfCreditCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeleteLetterOfCreditCommandHandler
    : IRequestHandler<DeleteLetterOfCreditCommand, ApiResponse>
{
    private readonly IRepository<LetterOfCredit, long> _repo;
    private readonly IUnitOfWork _uow;

    public DeleteLetterOfCreditCommandHandler(IRepository<LetterOfCredit, long> repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse> Handle(DeleteLetterOfCreditCommand cmd, CancellationToken ct)
    {
        var lc = await _repo.GetByIdAsync(cmd.Id, ct);
        if (lc is null) return ApiResponse.Fail("Letter of credit not found.");
        if (lc.Status is not (LcStatus.Draft or LcStatus.Cancelled))
            return ApiResponse.Fail("Only Draft or Cancelled LCs can be deleted.");

        _repo.Remove(lc);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Letter of credit deleted.");
    }
}
