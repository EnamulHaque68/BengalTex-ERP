using BengalTex.ERP.Application.Accounting;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Banking.Commands;

// ═══════════════════════════ DTOs ═══════════════════════════

public sealed record LcFinancialEventDto(
    long Id, string EventType, DateOnly EventDate, decimal Amount, decimal MarginApplied,
    string PaymentMethod, string? Reference, string? Notes);

/// <summary>Running balances derived from an LC's financial events.</summary>
public sealed record LcEventsSummaryDto(
    decimal MarginBalance,          // Σ deposits − Σ applied
    decimal PadOutstanding,         // Σ sight-financed − Σ PAD settled
    decimal AcceptanceOutstanding,  // Σ usance-financed − Σ acceptance settled
    decimal TotalCharges,
    decimal TotalInterest);

public sealed record LcEventsResultDto(
    IReadOnlyList<LcFinancialEventDto> Events, LcEventsSummaryDto Summary);

// ═══════════════════════════ Query ═══════════════════════════

public sealed record GetLcFinancialEventsQuery(long LcId) : IRequest<ApiResponse<LcEventsResultDto>>;

internal sealed class GetLcFinancialEventsQueryHandler
    : IRequestHandler<GetLcFinancialEventsQuery, ApiResponse<LcEventsResultDto>>
{
    private readonly IRepository<LcFinancialEvent, long> _repo;
    public GetLcFinancialEventsQueryHandler(IRepository<LcFinancialEvent, long> repo) => _repo = repo;

    public async Task<ApiResponse<LcEventsResultDto>> Handle(GetLcFinancialEventsQuery q, CancellationToken ct)
    {
        var events = await _repo.Query().AsNoTracking()
            .Where(e => e.LetterOfCreditId == q.LcId)
            .OrderBy(e => e.EventDate).ThenBy(e => e.Id)
            .ToListAsync(ct);

        var dtos = events.Select(e => new LcFinancialEventDto(
            e.Id, e.EventType.ToString(), e.EventDate, e.Amount, e.MarginApplied,
            e.PaymentMethod.ToString(), e.Reference, e.Notes)).ToList();

        return ApiResponse<LcEventsResultDto>.Ok(new LcEventsResultDto(dtos, LcEventMath.Summarize(events)));
    }
}

// ═══════════════════════════ Shared math + posting ═══════════════════════════

internal static class LcEventMath
{
    public static LcEventsSummaryDto Summarize(IEnumerable<LcFinancialEvent> events)
    {
        decimal marginDep = 0, marginApp = 0, padFin = 0, padSet = 0, accFin = 0, accSet = 0, charges = 0, interest = 0;
        foreach (var e in events)
        {
            switch (e.EventType)
            {
                case LcEventType.MarginDeposit: marginDep += e.Amount; break;
                case LcEventType.BankCharge: charges += e.Amount; break;
                case LcEventType.Interest: interest += e.Amount; break;
                case LcEventType.RetirementSight: marginApp += e.MarginApplied; padFin += e.Amount - e.MarginApplied; break;
                case LcEventType.AcceptanceUsance: marginApp += e.MarginApplied; accFin += e.Amount - e.MarginApplied; break;
                case LcEventType.PadSettlement: padSet += e.Amount; break;
                case LcEventType.AcceptanceSettlement: accSet += e.Amount; break;
            }
        }
        return new LcEventsSummaryDto(
            Math.Round(marginDep - marginApp, 2),
            Math.Round(padFin - padSet, 2),
            Math.Round(accFin - accSet, 2),
            Math.Round(charges, 2),
            Math.Round(interest, 2));
    }
}

// ═══════════════════════════ Add event (posts the journal) ═══════════════════════════

public sealed record AddLcFinancialEventCommand(
    long LcId, string EventType, DateOnly EventDate, decimal Amount, decimal MarginApplied,
    string PaymentMethod, string? Reference, string? Notes) : IRequest<ApiResponse<long>>;

public sealed class AddLcFinancialEventCommandValidator : AbstractValidator<AddLcFinancialEventCommand>
{
    public AddLcFinancialEventCommandValidator()
    {
        RuleFor(x => x.LcId).GreaterThan(0);
        RuleFor(x => x.EventType).NotEmpty()
            .Must(t => Enum.TryParse<LcEventType>(t, out _)).WithMessage("Unknown LC event type.");
        RuleFor(x => x.EventDate).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.MarginApplied).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PaymentMethod).NotEmpty()
            .Must(pm => Enum.TryParse<PaymentMethod>(pm, out _)).WithMessage("Invalid payment method.");
        RuleFor(x => x.Reference).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

internal sealed class AddLcFinancialEventCommandHandler : IRequestHandler<AddLcFinancialEventCommand, ApiResponse<long>>
{
    private readonly IRepository<LcFinancialEvent, long> _repo;
    private readonly IRepository<LetterOfCredit, long> _lcRepo;
    private readonly IUnitOfWork _uow;
    private readonly IJournalPostingService _journal;

    public AddLcFinancialEventCommandHandler(
        IRepository<LcFinancialEvent, long> repo, IRepository<LetterOfCredit, long> lcRepo,
        IUnitOfWork uow, IJournalPostingService journal)
    {
        _repo = repo; _lcRepo = lcRepo; _uow = uow; _journal = journal;
    }

    public async Task<ApiResponse<long>> Handle(AddLcFinancialEventCommand cmd, CancellationToken ct)
    {
        var lc = await _lcRepo.GetByIdAsync(cmd.LcId, ct);
        if (lc is null) return ApiResponse<long>.Fail("Letter of credit not found.");
        if (lc.Status is LcStatus.Draft or LcStatus.Cancelled)
            return ApiResponse<long>.Fail("Open the LC before recording financial events.");

        var type = Enum.Parse<LcEventType>(cmd.EventType);
        var method = Enum.Parse<PaymentMethod>(cmd.PaymentMethod);
        var amount = Math.Round(cmd.Amount, 2, MidpointRounding.AwayFromZero);
        var isRetirement = type is LcEventType.RetirementSight or LcEventType.AcceptanceUsance;
        var marginApplied = isRetirement ? Math.Round(cmd.MarginApplied, 2, MidpointRounding.AwayFromZero) : 0m;
        if (marginApplied > amount)
            return ApiResponse<long>.Fail("Margin applied cannot exceed the retirement amount.");

        var entity = new LcFinancialEvent
        {
            LetterOfCreditId = lc.Id,
            EventType = type,
            EventDate = cmd.EventDate,
            Amount = amount,
            MarginApplied = marginApplied,
            PaymentMethod = method,
            Reference = string.IsNullOrWhiteSpace(cmd.Reference) ? null : cmd.Reference.Trim(),
            Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim()
        };
        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);   // persist so the Id exists for the journal source link

        var cash = method == PaymentMethod.Cash ? LedgerAccounts.Cash : LedgerAccounts.Bank;
        var lines = new List<JournalPostingLine>();
        switch (type)
        {
            case LcEventType.MarginDeposit:
                lines.Add(new(LedgerAccounts.LcMargin, amount, 0m));
                lines.Add(new(cash, 0m, amount));
                break;
            case LcEventType.BankCharge:
                lines.Add(new(LedgerAccounts.BankCharges, amount, 0m));
                lines.Add(new(cash, 0m, amount));
                break;
            case LcEventType.Interest:
                lines.Add(new(LedgerAccounts.InterestExpense, amount, 0m));
                lines.Add(new(cash, 0m, amount));
                break;
            case LcEventType.RetirementSight:
            case LcEventType.AcceptanceUsance:
                var financed = amount - marginApplied;
                var liability = type == LcEventType.RetirementSight
                    ? LedgerAccounts.PadLiability : LedgerAccounts.AcceptanceLiability;
                lines.Add(new(LedgerAccounts.AccountsPayable, amount, 0m));     // supplier payable cleared
                if (marginApplied > 0m) lines.Add(new(LedgerAccounts.LcMargin, 0m, marginApplied)); // margin applied
                if (financed > 0m) lines.Add(new(liability, 0m, financed));     // bank finances the rest
                if (lc.Status == LcStatus.Open) { lc.Status = LcStatus.Shipped; lc.ShipmentDate ??= cmd.EventDate; _lcRepo.Update(lc); }
                break;
            case LcEventType.PadSettlement:
                lines.Add(new(LedgerAccounts.PadLiability, amount, 0m));
                lines.Add(new(cash, 0m, amount));
                break;
            case LcEventType.AcceptanceSettlement:
                lines.Add(new(LedgerAccounts.AcceptanceLiability, amount, 0m));
                lines.Add(new(cash, 0m, amount));
                break;
        }

        await _journal.PostAsync(
            cmd.EventDate,
            $"LC {lc.LcNumber} — {type} ({lc.Code})",
            "LcFinancialEvent", entity.Id, lc.Code, lines, ct);

        // Auto-settle the LC once every bank liability is cleared.
        if (type is LcEventType.PadSettlement or LcEventType.AcceptanceSettlement && lc.Status == LcStatus.Shipped)
        {
            var all = await _repo.Query().AsNoTracking().Where(e => e.LetterOfCreditId == lc.Id).ToListAsync(ct);
            var s = LcEventMath.Summarize(all);
            if (s.PadOutstanding <= 0m && s.AcceptanceOutstanding <= 0m)
            {
                lc.Status = LcStatus.Settled;
                lc.SettlementDate ??= cmd.EventDate;
                _lcRepo.Update(lc);
            }
        }

        await _uow.SaveChangesAsync(ct);
        return ApiResponse<long>.Ok(entity.Id, $"LC event '{type}' recorded.");
    }
}
