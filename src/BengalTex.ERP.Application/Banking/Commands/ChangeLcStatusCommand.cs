using BengalTex.ERP.Application.Banking.Dtos;
using BengalTex.ERP.Application.Banking.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.Banking.Commands;

/// <summary>
/// Drives the LC lifecycle. Action ∈ Open | Ship | Settle | Cancel:
///   Open   Draft → Open ; Ship   Open → Shipped (sets ShipmentDate) ;
///   Settle Shipped → Settled (sets SettlementDate) ; Cancel Draft|Open → Cancelled.
/// </summary>
public sealed record ChangeLcStatusCommand(long Id, string Action, DateOnly? Date)
    : IRequest<ApiResponse<LetterOfCreditDto>>;

internal sealed class ChangeLcStatusCommandHandler
    : IRequestHandler<ChangeLcStatusCommand, ApiResponse<LetterOfCreditDto>>
{
    private readonly IRepository<LetterOfCredit, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public ChangeLcStatusCommandHandler(IRepository<LetterOfCredit, long> repo, IUnitOfWork uow, IMediator mediator)
    {
        _repo = repo;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<LetterOfCreditDto>> Handle(ChangeLcStatusCommand cmd, CancellationToken ct)
    {
        var lc = await _repo.GetByIdAsync(cmd.Id, ct);
        if (lc is null) return ApiResponse<LetterOfCreditDto>.Fail("Letter of credit not found.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        switch (cmd.Action?.ToLowerInvariant())
        {
            case "open":
                if (lc.Status != LcStatus.Draft) return Fail("Only a Draft LC can be opened.");
                lc.Status = LcStatus.Open;
                break;
            case "ship":
                if (lc.Status != LcStatus.Open) return Fail("Only an Open LC can be marked shipped.");
                lc.Status = LcStatus.Shipped;
                lc.ShipmentDate = cmd.Date ?? today;
                break;
            case "settle":
                if (lc.Status != LcStatus.Shipped) return Fail("Only a Shipped LC can be settled.");
                lc.Status = LcStatus.Settled;
                lc.SettlementDate = cmd.Date ?? today;
                break;
            case "cancel":
                if (lc.Status is not (LcStatus.Draft or LcStatus.Open)) return Fail("Only Draft or Open LCs can be cancelled.");
                lc.Status = LcStatus.Cancelled;
                break;
            default:
                return Fail($"Unknown action '{cmd.Action}'.");
        }

        _repo.Update(lc);
        await _uow.SaveChangesAsync(ct);
        return await _mediator.Send(new GetLetterOfCreditByIdQuery(lc.Id), ct);

        static ApiResponse<LetterOfCreditDto> Fail(string m) => ApiResponse<LetterOfCreditDto>.Fail(m);
    }
}
