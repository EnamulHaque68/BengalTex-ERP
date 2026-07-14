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

public sealed record BankFacilityDto(
    long Id, string Code, string FacilityType, string BankName, string? AccountReference,
    decimal Amount, decimal InterestRate, DateOnly StartDate, DateOnly? MaturityDate, string Status, string? Notes);

public sealed record BankFacilityEventDto(
    long Id, string EventType, DateOnly EventDate, decimal Amount, string PaymentMethod, string? Reference, string? Notes);

public sealed record BankFacilitySummaryDto(
    decimal LoanOutstanding, decimal FdrBalance, decimal TotalInterestPaid, decimal TotalInterestIncome);

public sealed record BankFacilityDetailDto(
    BankFacilityDto Facility, IReadOnlyList<BankFacilityEventDto> Events, BankFacilitySummaryDto Summary);

// ═══════════════════════════ Shared math + validity ═══════════════════════════

internal static class BankFacilityMath
{
    public static BankFacilitySummaryDto Summarize(IEnumerable<BankFacilityEvent> events)
    {
        decimal draw = 0, repay = 0, place = 0, encash = 0, intPaid = 0, intInc = 0;
        foreach (var e in events)
            switch (e.EventType)
            {
                case BankFacilityEventType.Drawdown: draw += e.Amount; break;
                case BankFacilityEventType.PrincipalRepayment: repay += e.Amount; break;
                case BankFacilityEventType.FdrPlacement: place += e.Amount; break;
                case BankFacilityEventType.FdrEncashment: encash += e.Amount; break;
                case BankFacilityEventType.InterestCharge: intPaid += e.Amount; break;
                case BankFacilityEventType.FdrInterestIncome: intInc += e.Amount; break;
            }
        return new BankFacilitySummaryDto(
            Math.Round(draw - repay, 2), Math.Round(place - encash, 2),
            Math.Round(intPaid, 2), Math.Round(intInc, 2));
    }

    /// <summary>FDR events belong to FDR facilities; loan/OD events to term-loan / overdraft facilities.</summary>
    public static bool IsValidFor(BankFacilityType facility, BankFacilityEventType ev)
    {
        var fdrEvent = ev is BankFacilityEventType.FdrPlacement or BankFacilityEventType.FdrInterestIncome or BankFacilityEventType.FdrEncashment;
        return facility == BankFacilityType.Fdr ? fdrEvent : !fdrEvent;
    }
}

// ═══════════════════════════ Queries ═══════════════════════════

public sealed record GetBankFacilitiesQuery(string? Status = null) : IRequest<ApiResponse<IReadOnlyList<BankFacilityDto>>>;

internal sealed class GetBankFacilitiesQueryHandler
    : IRequestHandler<GetBankFacilitiesQuery, ApiResponse<IReadOnlyList<BankFacilityDto>>>
{
    private readonly IRepository<BankFacility, long> _repo;
    public GetBankFacilitiesQueryHandler(IRepository<BankFacility, long> repo) => _repo = repo;

    public async Task<ApiResponse<IReadOnlyList<BankFacilityDto>>> Handle(GetBankFacilitiesQuery q, CancellationToken ct)
    {
        var query = _repo.Query().AsNoTracking();
        if (!string.IsNullOrEmpty(q.Status) && Enum.TryParse<BankFacilityStatus>(q.Status, out var s))
            query = query.Where(f => f.Status == s);

        var rows = await query.OrderByDescending(f => f.StartDate).ThenByDescending(f => f.Id)
            .Select(f => new BankFacilityDto(
                f.Id, f.Code, f.FacilityType.ToString(), f.BankName, f.AccountReference,
                f.Amount, f.InterestRate, f.StartDate, f.MaturityDate, f.Status.ToString(), f.Notes))
            .ToListAsync(ct);
        return ApiResponse<IReadOnlyList<BankFacilityDto>>.Ok(rows);
    }
}

public sealed record GetBankFacilityByIdQuery(long Id) : IRequest<ApiResponse<BankFacilityDetailDto>>;

internal sealed class GetBankFacilityByIdQueryHandler
    : IRequestHandler<GetBankFacilityByIdQuery, ApiResponse<BankFacilityDetailDto>>
{
    private readonly IRepository<BankFacility, long> _repo;
    private readonly IRepository<BankFacilityEvent, long> _eventRepo;

    public GetBankFacilityByIdQueryHandler(IRepository<BankFacility, long> repo, IRepository<BankFacilityEvent, long> eventRepo)
    {
        _repo = repo; _eventRepo = eventRepo;
    }

    public async Task<ApiResponse<BankFacilityDetailDto>> Handle(GetBankFacilityByIdQuery q, CancellationToken ct)
    {
        var f = await _repo.Query().AsNoTracking().FirstOrDefaultAsync(x => x.Id == q.Id, ct);
        if (f is null) return ApiResponse<BankFacilityDetailDto>.Fail("Bank facility not found.");

        var events = await _eventRepo.Query().AsNoTracking()
            .Where(e => e.BankFacilityId == q.Id)
            .OrderBy(e => e.EventDate).ThenBy(e => e.Id)
            .ToListAsync(ct);

        var dto = new BankFacilityDto(
            f.Id, f.Code, f.FacilityType.ToString(), f.BankName, f.AccountReference,
            f.Amount, f.InterestRate, f.StartDate, f.MaturityDate, f.Status.ToString(), f.Notes);
        var eventDtos = events.Select(e => new BankFacilityEventDto(
            e.Id, e.EventType.ToString(), e.EventDate, e.Amount, e.PaymentMethod.ToString(), e.Reference, e.Notes)).ToList();

        return ApiResponse<BankFacilityDetailDto>.Ok(
            new BankFacilityDetailDto(dto, eventDtos, BankFacilityMath.Summarize(events)));
    }
}

// ═══════════════════════════ Create facility ═══════════════════════════

public sealed record CreateBankFacilityCommand(
    string FacilityType, string BankName, string? AccountReference, decimal Amount,
    decimal InterestRate, DateOnly StartDate, DateOnly? MaturityDate, string? Notes) : IRequest<ApiResponse<long>>;

public sealed class CreateBankFacilityCommandValidator : AbstractValidator<CreateBankFacilityCommand>
{
    public CreateBankFacilityCommandValidator()
    {
        RuleFor(x => x.FacilityType).NotEmpty()
            .Must(t => Enum.TryParse<BankFacilityType>(t, out _)).WithMessage("Type must be TermLoan, OverdraftCC, or Fdr.");
        RuleFor(x => x.BankName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AccountReference).MaximumLength(100);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.InterestRate).GreaterThanOrEqualTo(0);
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

internal sealed class CreateBankFacilityCommandHandler : IRequestHandler<CreateBankFacilityCommand, ApiResponse<long>>
{
    private readonly IRepository<BankFacility, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;

    public CreateBankFacilityCommandHandler(IRepository<BankFacility, long> repo, IUnitOfWork uow, INumberingService numbering)
    {
        _repo = repo; _uow = uow; _numbering = numbering;
    }

    public async Task<ApiResponse<long>> Handle(CreateBankFacilityCommand cmd, CancellationToken ct)
    {
        var code = await _numbering.NextAsync("BF", null, ct);
        var entity = new BankFacility
        {
            Code = code,
            FacilityType = Enum.Parse<BankFacilityType>(cmd.FacilityType),
            BankName = cmd.BankName.Trim(),
            AccountReference = string.IsNullOrWhiteSpace(cmd.AccountReference) ? null : cmd.AccountReference.Trim(),
            Amount = cmd.Amount,
            InterestRate = cmd.InterestRate,
            StartDate = cmd.StartDate,
            MaturityDate = cmd.MaturityDate,
            Status = BankFacilityStatus.Active,
            Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim()
        };
        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<long>.Ok(entity.Id, "Bank facility created.");
    }
}

// ═══════════════════════════ Add event (posts the journal) ═══════════════════════════

public sealed record AddBankFacilityEventCommand(
    long FacilityId, string EventType, DateOnly EventDate, decimal Amount,
    string PaymentMethod, string? Reference, string? Notes) : IRequest<ApiResponse<long>>;

public sealed class AddBankFacilityEventCommandValidator : AbstractValidator<AddBankFacilityEventCommand>
{
    public AddBankFacilityEventCommandValidator()
    {
        RuleFor(x => x.FacilityId).GreaterThan(0);
        RuleFor(x => x.EventType).NotEmpty()
            .Must(t => Enum.TryParse<BankFacilityEventType>(t, out _)).WithMessage("Unknown facility event type.");
        RuleFor(x => x.EventDate).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.PaymentMethod).NotEmpty()
            .Must(pm => Enum.TryParse<PaymentMethod>(pm, out _)).WithMessage("Invalid payment method.");
        RuleFor(x => x.Reference).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

internal sealed class AddBankFacilityEventCommandHandler : IRequestHandler<AddBankFacilityEventCommand, ApiResponse<long>>
{
    private readonly IRepository<BankFacilityEvent, long> _repo;
    private readonly IRepository<BankFacility, long> _facilityRepo;
    private readonly IUnitOfWork _uow;
    private readonly IJournalPostingService _journal;

    public AddBankFacilityEventCommandHandler(
        IRepository<BankFacilityEvent, long> repo, IRepository<BankFacility, long> facilityRepo,
        IUnitOfWork uow, IJournalPostingService journal)
    {
        _repo = repo; _facilityRepo = facilityRepo; _uow = uow; _journal = journal;
    }

    public async Task<ApiResponse<long>> Handle(AddBankFacilityEventCommand cmd, CancellationToken ct)
    {
        var facility = await _facilityRepo.GetByIdAsync(cmd.FacilityId, ct);
        if (facility is null) return ApiResponse<long>.Fail("Bank facility not found.");
        if (facility.Status == BankFacilityStatus.Closed)
            return ApiResponse<long>.Fail("This facility is closed.");

        var type = Enum.Parse<BankFacilityEventType>(cmd.EventType);
        if (!BankFacilityMath.IsValidFor(facility.FacilityType, type))
            return ApiResponse<long>.Fail($"Event '{type}' is not valid for a {facility.FacilityType} facility.");

        var method = Enum.Parse<PaymentMethod>(cmd.PaymentMethod);
        var amount = Math.Round(cmd.Amount, 2, MidpointRounding.AwayFromZero);

        var entity = new BankFacilityEvent
        {
            BankFacilityId = facility.Id,
            EventType = type,
            EventDate = cmd.EventDate,
            Amount = amount,
            PaymentMethod = method,
            Reference = string.IsNullOrWhiteSpace(cmd.Reference) ? null : cmd.Reference.Trim(),
            Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim()
        };
        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);

        var cash = method == PaymentMethod.Cash ? LedgerAccounts.Cash : LedgerAccounts.Bank;
        var lines = type switch
        {
            BankFacilityEventType.Drawdown => new[]
                { new JournalPostingLine(cash, amount, 0m), new JournalPostingLine(LedgerAccounts.BankLoan, 0m, amount) },
            BankFacilityEventType.PrincipalRepayment => new[]
                { new JournalPostingLine(LedgerAccounts.BankLoan, amount, 0m), new JournalPostingLine(cash, 0m, amount) },
            BankFacilityEventType.InterestCharge => new[]
                { new JournalPostingLine(LedgerAccounts.InterestExpense, amount, 0m), new JournalPostingLine(cash, 0m, amount) },
            BankFacilityEventType.FdrPlacement => new[]
                { new JournalPostingLine(LedgerAccounts.FixedDeposits, amount, 0m), new JournalPostingLine(cash, 0m, amount) },
            BankFacilityEventType.FdrEncashment => new[]
                { new JournalPostingLine(cash, amount, 0m), new JournalPostingLine(LedgerAccounts.FixedDeposits, 0m, amount) },
            BankFacilityEventType.FdrInterestIncome => new[]
                { new JournalPostingLine(cash, amount, 0m), new JournalPostingLine(LedgerAccounts.OtherIncome, 0m, amount) },
            _ => Array.Empty<JournalPostingLine>()
        };

        await _journal.PostAsync(
            cmd.EventDate, $"Facility {facility.Code} — {type}", "BankFacilityEvent", entity.Id, facility.Code, lines, ct);

        // Auto-close a fully repaid loan / fully encashed FDR.
        var all = await _repo.Query().AsNoTracking().Where(e => e.BankFacilityId == facility.Id).ToListAsync(ct);
        var s = BankFacilityMath.Summarize(all);
        var settled = facility.FacilityType == BankFacilityType.Fdr ? s.FdrBalance <= 0m : s.LoanOutstanding <= 0m;
        var settlingEvent = type is BankFacilityEventType.PrincipalRepayment or BankFacilityEventType.FdrEncashment;
        if (settled && settlingEvent)
        {
            facility.Status = BankFacilityStatus.Closed;
            _facilityRepo.Update(facility);
        }

        await _uow.SaveChangesAsync(ct);
        return ApiResponse<long>.Ok(entity.Id, $"Facility event '{type}' recorded.");
    }
}
