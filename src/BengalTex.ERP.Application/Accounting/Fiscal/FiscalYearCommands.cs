using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Accounting.Fiscal;

// ═══════════════════════════ DTOs ═══════════════════════════

public sealed record AccountingPeriodDto(
    int Id, int PeriodNumber, string Name, DateOnly StartDate, DateOnly EndDate,
    string Status, DateTimeOffset? StatusChangedAt, string? StatusChangedBy);

public sealed record FinancialYearDto(
    int Id, string Code, DateOnly StartDate, DateOnly EndDate, string Status,
    DateTimeOffset? ClosedAt, string? ClosedBy, string? Notes,
    IReadOnlyList<AccountingPeriodDto> Periods);

/// <summary>Preview of the year-end closing voucher — shown before the user confirms the close.</summary>
public sealed record YearClosePreviewDto(
    decimal TotalIncome, decimal TotalExpense, decimal NetIncome, int AccountCount);

// ═══════════════════════════ Queries ═══════════════════════════

public sealed record GetFinancialYearsQuery : IRequest<ApiResponse<IReadOnlyList<FinancialYearDto>>>;

internal sealed class GetFinancialYearsQueryHandler
    : IRequestHandler<GetFinancialYearsQuery, ApiResponse<IReadOnlyList<FinancialYearDto>>>
{
    private readonly IRepository<FinancialYear> _repo;
    public GetFinancialYearsQueryHandler(IRepository<FinancialYear> repo) => _repo = repo;

    public async Task<ApiResponse<IReadOnlyList<FinancialYearDto>>> Handle(
        GetFinancialYearsQuery request, CancellationToken ct)
    {
        var years = await _repo.Query().AsNoTracking()
            .Include(f => f.Periods)
            .OrderByDescending(f => f.StartDate)
            .ToListAsync(ct);

        var dtos = years.Select(f => new FinancialYearDto(
                f.Id, f.Code, f.StartDate, f.EndDate, f.Status.ToString(),
                f.ClosedAt, f.ClosedBy, f.Notes,
                f.Periods.OrderBy(p => p.PeriodNumber).Select(p => new AccountingPeriodDto(
                    p.Id, p.PeriodNumber, p.Name, p.StartDate, p.EndDate,
                    p.Status.ToString(), p.StatusChangedAt, p.StatusChangedBy)).ToList()))
            .ToList();

        return ApiResponse<IReadOnlyList<FinancialYearDto>>.Ok(dtos);
    }
}

/// <summary>Income/expense totals that the year-end close would sweep into Retained Earnings.</summary>
public sealed record GetYearClosePreviewQuery(int FinancialYearId) : IRequest<ApiResponse<YearClosePreviewDto>>;

internal sealed class GetYearClosePreviewQueryHandler
    : IRequestHandler<GetYearClosePreviewQuery, ApiResponse<YearClosePreviewDto>>
{
    private readonly IRepository<FinancialYear> _fyRepo;
    private readonly IRepository<JournalEntryLine, long> _lineRepo;

    public GetYearClosePreviewQueryHandler(
        IRepository<FinancialYear> fyRepo, IRepository<JournalEntryLine, long> lineRepo)
    {
        _fyRepo = fyRepo;
        _lineRepo = lineRepo;
    }

    public async Task<ApiResponse<YearClosePreviewDto>> Handle(GetYearClosePreviewQuery q, CancellationToken ct)
    {
        var fy = await _fyRepo.GetByIdAsync(q.FinancialYearId, ct);
        if (fy is null) return ApiResponse<YearClosePreviewDto>.Fail("Financial year not found.");

        var nets = await YearCloseCalc.NetPlBalancesAsync(_lineRepo, fy, ct);
        var income = nets.Where(n => n.Type == AccountType.Income).Sum(n => n.Net);
        var expense = nets.Where(n => n.Type == AccountType.Expense).Sum(n => n.Net);

        return ApiResponse<YearClosePreviewDto>.Ok(
            new YearClosePreviewDto(income, expense, income - expense, nets.Count));
    }
}

// ═══════════════════════════ Commands ═══════════════════════════

/// <summary>
/// Creates a fiscal year and auto-generates its 12 monthly periods. Start date must be the
/// first of a month; the year ends exactly 12 months later.
/// </summary>
public sealed record CreateFinancialYearCommand(string Code, DateOnly StartDate, string? Notes)
    : IRequest<ApiResponse<int>>;

public sealed class CreateFinancialYearCommandValidator : AbstractValidator<CreateFinancialYearCommand>
{
    public CreateFinancialYearCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.StartDate).NotEmpty()
            .Must(d => d.Day == 1).WithMessage("The fiscal year must start on the first day of a month.");
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

internal sealed class CreateFinancialYearCommandHandler
    : IRequestHandler<CreateFinancialYearCommand, ApiResponse<int>>
{
    private readonly IRepository<FinancialYear> _repo;
    private readonly IUnitOfWork _uow;

    public CreateFinancialYearCommandHandler(IRepository<FinancialYear> repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ApiResponse<int>> Handle(CreateFinancialYearCommand cmd, CancellationToken ct)
    {
        var start = cmd.StartDate;
        var end = start.AddYears(1).AddDays(-1);

        if (await _repo.AnyAsync(f => f.Code == cmd.Code.Trim(), ct))
            return ApiResponse<int>.Fail($"Financial year '{cmd.Code}' already exists.");
        if (await _repo.AnyAsync(f => f.StartDate <= end && f.EndDate >= start, ct))
            return ApiResponse<int>.Fail("The date range overlaps an existing financial year.");

        var fy = new FinancialYear
        {
            Code = cmd.Code.Trim(),
            StartDate = start,
            EndDate = end,
            Status = FinancialYearStatus.Open,
            Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim()
        };

        for (var i = 0; i < 12; i++)
        {
            var pStart = start.AddMonths(i);
            var pEnd = pStart.AddMonths(1).AddDays(-1);
            fy.Periods.Add(new AccountingPeriod
            {
                PeriodNumber = i + 1,
                Name = pStart.ToString("MMM yyyy"),
                StartDate = pStart,
                EndDate = pEnd,
                Status = AccountingPeriodStatus.Open
            });
        }

        await _repo.AddAsync(fy, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(fy.Id, $"Financial year {fy.Code} created with 12 periods.");
    }
}

/// <summary>Period lifecycle: soft-close, lock, or reopen one accounting period (each audited via the standard audit trail).</summary>
public sealed record ChangePeriodStatusCommand(int PeriodId, string Action) : IRequest<ApiResponse>;

internal sealed class ChangePeriodStatusCommandHandler : IRequestHandler<ChangePeriodStatusCommand, ApiResponse>
{
    private readonly IRepository<AccountingPeriod> _repo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public ChangePeriodStatusCommandHandler(
        IRepository<AccountingPeriod> repo, IUnitOfWork uow, ICurrentUserService currentUser)
    {
        _repo = repo;
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse> Handle(ChangePeriodStatusCommand cmd, CancellationToken ct)
    {
        var period = await _repo.Query()
            .Include(p => p.FinancialYear)
            .FirstOrDefaultAsync(p => p.Id == cmd.PeriodId, ct);
        if (period is null) return ApiResponse.Fail("Accounting period not found.");
        if (period.FinancialYear.Status == FinancialYearStatus.Closed)
            return ApiResponse.Fail("The financial year is closed — reopen the year first.");

        var target = cmd.Action?.ToLowerInvariant() switch
        {
            "soft-close" => AccountingPeriodStatus.SoftClosed,
            "lock" => AccountingPeriodStatus.Locked,
            "reopen" => AccountingPeriodStatus.Open,
            _ => (AccountingPeriodStatus?)null
        };
        if (target is null) return ApiResponse.Fail("Unknown action — use soft-close, lock or reopen.");
        if (period.Status == target) return ApiResponse.Fail($"Period is already {period.Status}.");

        period.Status = target.Value;
        period.StatusChangedAt = DateTimeOffset.UtcNow;
        period.StatusChangedBy = _currentUser.UserName;
        _repo.Update(period);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse.Ok($"Period {period.Name} is now {target}.");
    }
}

/// <summary>
/// Year-end close: sweeps every Income/Expense account's net balance for the year into
/// Retained Earnings (3200) via a Closing voucher (excluded from period P&amp;L / TB reports),
/// then freezes the year. Requires all 12 periods to be Locked.
/// </summary>
public sealed record CloseFinancialYearCommand(int Id) : IRequest<ApiResponse>;

internal sealed class CloseFinancialYearCommandHandler : IRequestHandler<CloseFinancialYearCommand, ApiResponse>
{
    private readonly IRepository<FinancialYear> _fyRepo;
    private readonly IRepository<JournalEntry, long> _journalRepo;
    private readonly IRepository<JournalEntryLine, long> _lineRepo;
    private readonly IRepository<Domain.Entities.Account> _accountRepo;
    private readonly IPeriodGuard _periodGuard;
    private readonly INumberingService _numbering;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public CloseFinancialYearCommandHandler(
        IRepository<FinancialYear> fyRepo,
        IRepository<JournalEntry, long> journalRepo,
        IRepository<JournalEntryLine, long> lineRepo,
        IRepository<Domain.Entities.Account> accountRepo,
        IPeriodGuard periodGuard,
        INumberingService numbering,
        ICurrentUserService currentUser,
        IUnitOfWork uow)
    {
        _fyRepo = fyRepo;
        _journalRepo = journalRepo;
        _lineRepo = lineRepo;
        _accountRepo = accountRepo;
        _periodGuard = periodGuard;
        _numbering = numbering;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task<ApiResponse> Handle(CloseFinancialYearCommand cmd, CancellationToken ct)
    {
        var fy = await _fyRepo.Query()
            .Include(f => f.Periods)
            .FirstOrDefaultAsync(f => f.Id == cmd.Id, ct);
        if (fy is null) return ApiResponse.Fail("Financial year not found.");
        if (fy.Status == FinancialYearStatus.Closed) return ApiResponse.Fail("This year is already closed.");

        var unlocked = fy.Periods.Where(p => p.Status != AccountingPeriodStatus.Locked)
            .Select(p => p.Name).ToList();
        if (unlocked.Count > 0)
            return ApiResponse.Fail("All periods must be locked before closing the year — open: " +
                                    string.Join(", ", unlocked));

        var nets = await YearCloseCalc.NetPlBalancesAsync(_lineRepo, fy, ct);
        var retainedEarnings = await _accountRepo.Query()
            .FirstOrDefaultAsync(a => a.Code == Accounting.LedgerAccounts.RetainedEarnings, ct);
        if (retainedEarnings is null)
            return ApiResponse.Fail("Retained Earnings account (3200) not found in the chart of accounts.");

        // Build the closing legs: zero every P&L account against Retained Earnings.
        var lines = new List<JournalEntryLine>();
        var sort = 0;
        decimal netIncome = 0m;
        foreach (var n in nets.Where(n => n.Net != 0m))
        {
            if (n.Type == AccountType.Income)
            {
                // Income is credit-normal: debit its net credit balance to zero it (negative → credit).
                lines.Add(new JournalEntryLine
                {
                    AccountId = n.AccountId,
                    Debit = n.Net > 0 ? n.Net : 0m,
                    Credit = n.Net < 0 ? -n.Net : 0m,
                    SortOrder = sort++
                });
                netIncome += n.Net;
            }
            else
            {
                // Expense is debit-normal: credit its net debit balance to zero it.
                lines.Add(new JournalEntryLine
                {
                    AccountId = n.AccountId,
                    Debit = n.Net < 0 ? -n.Net : 0m,
                    Credit = n.Net > 0 ? n.Net : 0m,
                    SortOrder = sort++
                });
                netIncome -= n.Net;
            }
        }

        if (lines.Count > 0)
        {
            lines.Add(new JournalEntryLine
            {
                AccountId = retainedEarnings.Id,
                Debit = netIncome < 0 ? -netIncome : 0m,
                Credit = netIncome > 0 ? netIncome : 0m,
                SortOrder = sort
            });

            var entry = new JournalEntry
            {
                Code = await _numbering.NextAsync("CL", null, ct),
                EntryDate = fy.EndDate,
                Narration = $"Year-end close {fy.Code} — net {(netIncome >= 0 ? "profit" : "loss")} {Math.Abs(netIncome):N2} to Retained Earnings",
                Status = JournalEntryStatus.Posted,
                VoucherType = VoucherType.Closing,     // excluded from period P&L / TB by design
                AccountingPeriodId = await _periodGuard.GetPeriodIdAsync(fy.EndDate, ct),
                SourceType = "YearEndClose",
                SourceId = fy.Id,
                SourceCode = fy.Code,
                PostedAt = DateTimeOffset.UtcNow,
                PostedBy = _currentUser.UserName ?? "system",
                Lines = lines
            };
            await _journalRepo.AddAsync(entry, ct);
        }

        fy.Status = FinancialYearStatus.Closed;
        fy.ClosedAt = DateTimeOffset.UtcNow;
        fy.ClosedBy = _currentUser.UserName;
        _fyRepo.Update(fy);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse.Ok($"Year {fy.Code} closed — net {(netIncome >= 0 ? "profit" : "loss")} {Math.Abs(netIncome):N2} moved to Retained Earnings.");
    }
}

/// <summary>Audited reopen: reverses the year-end closing voucher and re-opens the year.</summary>
public sealed record ReopenFinancialYearCommand(int Id, string Reason) : IRequest<ApiResponse>;

internal sealed class ReopenFinancialYearCommandHandler : IRequestHandler<ReopenFinancialYearCommand, ApiResponse>
{
    private readonly IRepository<FinancialYear> _fyRepo;
    private readonly IRepository<JournalEntry, long> _journalRepo;
    private readonly INumberingService _numbering;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public ReopenFinancialYearCommandHandler(
        IRepository<FinancialYear> fyRepo,
        IRepository<JournalEntry, long> journalRepo,
        INumberingService numbering,
        ICurrentUserService currentUser,
        IUnitOfWork uow)
    {
        _fyRepo = fyRepo;
        _journalRepo = journalRepo;
        _numbering = numbering;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task<ApiResponse> Handle(ReopenFinancialYearCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.Reason))
            return ApiResponse.Fail("A reason is required to reopen a closed year.");

        var fy = await _fyRepo.GetByIdAsync(cmd.Id, ct);
        if (fy is null) return ApiResponse.Fail("Financial year not found.");
        if (fy.Status != FinancialYearStatus.Closed) return ApiResponse.Fail("This year is not closed.");

        // Reverse the (latest un-reversed) closing voucher, if one was posted.
        var closing = await _journalRepo.Query()
            .Include(j => j.Lines)
            .Where(j => j.SourceType == "YearEndClose" && j.SourceId == fy.Id
                     && j.Status == JournalEntryStatus.Posted)
            .OrderByDescending(j => j.Id)
            .FirstOrDefaultAsync(ct);

        if (closing is not null)
        {
            var alreadyReversed = await _journalRepo.AnyAsync(j => j.ReversedEntryId == closing.Id, ct);
            if (!alreadyReversed)
            {
                var reversal = new JournalEntry
                {
                    Code = await _numbering.NextAsync("CL", null, ct),
                    EntryDate = closing.EntryDate,
                    Narration = $"Reversal of year-end close {fy.Code} (year reopened)",
                    Status = JournalEntryStatus.Posted,
                    VoucherType = VoucherType.Closing,
                    AccountingPeriodId = closing.AccountingPeriodId,
                    SourceType = "YearEndClose",
                    SourceId = fy.Id,
                    SourceCode = fy.Code,
                    ReversedEntryId = closing.Id,
                    ReversalReason = cmd.Reason.Trim(),
                    PostedAt = DateTimeOffset.UtcNow,
                    PostedBy = _currentUser.UserName ?? "system",
                    Lines = closing.Lines.OrderBy(l => l.SortOrder).Select((l, i) => new JournalEntryLine
                    {
                        AccountId = l.AccountId,
                        Debit = l.Credit,
                        Credit = l.Debit,
                        SortOrder = i
                    }).ToList()
                };
                await _journalRepo.AddAsync(reversal, ct);
            }
        }

        fy.Status = FinancialYearStatus.Open;
        fy.ClosedAt = null;
        fy.ClosedBy = null;
        fy.Notes = string.IsNullOrWhiteSpace(fy.Notes)
            ? $"Reopened: {cmd.Reason.Trim()}"
            : $"{fy.Notes}\nReopened: {cmd.Reason.Trim()}";
        _fyRepo.Update(fy);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse.Ok($"Year {fy.Code} reopened and its closing voucher reversed.");
    }
}

// ═══════════════════════════ Shared calc ═══════════════════════════

internal static class YearCloseCalc
{
    public sealed record PlNet(int AccountId, AccountType Type, decimal Net);

    /// <summary>
    /// Net P&amp;L balance per Income/Expense account within the year, EXCLUDING Closing
    /// vouchers (so a re-preview after reopen is correct). Income net = Cr − Dr; Expense = Dr − Cr.
    /// </summary>
    public static async Task<List<PlNet>> NetPlBalancesAsync(
        IRepository<JournalEntryLine, long> lineRepo, FinancialYear fy, CancellationToken ct)
    {
        var rows = await lineRepo.Query().AsNoTracking()
            .Where(l => l.JournalEntry.Status == JournalEntryStatus.Posted
                     && l.JournalEntry.VoucherType != VoucherType.Closing
                     && l.JournalEntry.EntryDate >= fy.StartDate
                     && l.JournalEntry.EntryDate <= fy.EndDate
                     && (l.Account.AccountType == AccountType.Income
                      || l.Account.AccountType == AccountType.Expense))
            .GroupBy(l => new { l.AccountId, l.Account.AccountType })
            .Select(g => new
            {
                g.Key.AccountId,
                g.Key.AccountType,
                Debit = g.Sum(x => x.Debit),
                Credit = g.Sum(x => x.Credit)
            })
            .ToListAsync(ct);

        return rows.Select(r => new PlNet(
                r.AccountId, r.AccountType,
                r.AccountType == AccountType.Income ? r.Credit - r.Debit : r.Debit - r.Credit))
            .ToList();
    }
}
