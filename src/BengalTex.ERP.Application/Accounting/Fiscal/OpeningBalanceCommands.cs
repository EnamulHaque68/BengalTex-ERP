using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Accounting.Fiscal;

// ═══════════════════════════ Template ═══════════════════════════

public sealed record OpeningBalanceAccountDto(
    int AccountId, string Code, string Name, string AccountType, decimal CurrentDebit, decimal CurrentCredit);

/// <summary>Postable accounts + any already-posted opening amounts — the import grid's rows.</summary>
public sealed record GetOpeningBalanceTemplateQuery : IRequest<ApiResponse<IReadOnlyList<OpeningBalanceAccountDto>>>;

internal sealed class GetOpeningBalanceTemplateQueryHandler
    : IRequestHandler<GetOpeningBalanceTemplateQuery, ApiResponse<IReadOnlyList<OpeningBalanceAccountDto>>>
{
    private readonly IRepository<Domain.Entities.Account> _accountRepo;
    private readonly IRepository<JournalEntry, long> _journalRepo;

    public GetOpeningBalanceTemplateQueryHandler(
        IRepository<Domain.Entities.Account> accountRepo, IRepository<JournalEntry, long> journalRepo)
    {
        _accountRepo = accountRepo;
        _journalRepo = journalRepo;
    }

    public async Task<ApiResponse<IReadOnlyList<OpeningBalanceAccountDto>>> Handle(
        GetOpeningBalanceTemplateQuery request, CancellationToken ct)
    {
        // Amounts already sitting on an active (un-reversed) opening voucher, per account.
        var opening = await _journalRepo.Query().AsNoTracking()
            .Where(j => j.SourceType == "OpeningBalance" && j.Status == JournalEntryStatus.Posted)
            .SelectMany(j => j.Lines)
            .GroupBy(l => l.AccountId)
            .Select(g => new { AccountId = g.Key, Debit = g.Sum(x => x.Debit), Credit = g.Sum(x => x.Credit) })
            .ToDictionaryAsync(x => x.AccountId, ct);

        var accounts = await _accountRepo.Query().AsNoTracking()
            .Where(a => !a.IsGroup && a.IsActive)
            .OrderBy(a => a.Code)
            .Select(a => new { a.Id, a.Code, a.Name, a.AccountType })
            .ToListAsync(ct);

        var rows = accounts.Select(a =>
        {
            opening.TryGetValue(a.Id, out var o);
            return new OpeningBalanceAccountDto(
                a.Id, a.Code, a.Name, a.AccountType.ToString(), o?.Debit ?? 0m, o?.Credit ?? 0m);
        }).ToList();

        return ApiResponse<IReadOnlyList<OpeningBalanceAccountDto>>.Ok(rows);
    }
}

// ═══════════════════════════ Import ═══════════════════════════

public sealed record OpeningBalanceLineInput(int AccountId, decimal Debit, decimal Credit);

/// <summary>
/// Imports go-live ledger opening balances as ONE posted Opening voucher (series OB). Any
/// imbalance is auto-plugged to Opening Balance Equity (3150). Re-import requires reversing
/// the existing opening voucher first. Party-wise AR/AP detail is carried by opening
/// invoices (<c>IsOpening</c> flag) whose journals are suppressed — the GL value lives here.
/// </summary>
public sealed record ImportOpeningBalancesCommand(
    DateOnly AsOfDate,
    IReadOnlyList<OpeningBalanceLineInput> Lines
) : IRequest<ApiResponse<long>>;

public sealed class ImportOpeningBalancesCommandValidator : AbstractValidator<ImportOpeningBalancesCommand>
{
    public ImportOpeningBalancesCommandValidator()
    {
        RuleFor(x => x.AsOfDate).NotEmpty();
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Provide at least one opening line.");
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(x => x.AccountId).GreaterThan(0);
            l.RuleFor(x => x.Debit).GreaterThanOrEqualTo(0);
            l.RuleFor(x => x.Credit).GreaterThanOrEqualTo(0);
            l.RuleFor(x => x).Must(x => !(x.Debit > 0 && x.Credit > 0))
                .WithMessage("A line may carry a debit or a credit, not both.");
        });
        RuleFor(x => x.Lines)
            .Must(ls => ls.Any(l => l.Debit > 0 || l.Credit > 0))
            .WithMessage("All lines are zero — nothing to import.");
        RuleFor(x => x.Lines)
            .Must(ls => ls.Select(l => l.AccountId).Distinct().Count() == ls.Count)
            .WithMessage("The same account appears more than once.");
    }
}

internal sealed class ImportOpeningBalancesCommandHandler
    : IRequestHandler<ImportOpeningBalancesCommand, ApiResponse<long>>
{
    private readonly IRepository<JournalEntry, long> _journalRepo;
    private readonly IRepository<Domain.Entities.Account> _accountRepo;
    private readonly IPeriodGuard _periodGuard;
    private readonly INumberingService _numbering;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public ImportOpeningBalancesCommandHandler(
        IRepository<JournalEntry, long> journalRepo,
        IRepository<Domain.Entities.Account> accountRepo,
        IPeriodGuard periodGuard,
        INumberingService numbering,
        ICurrentUserService currentUser,
        IUnitOfWork uow)
    {
        _journalRepo = journalRepo;
        _accountRepo = accountRepo;
        _periodGuard = periodGuard;
        _numbering = numbering;
        _currentUser = currentUser;
        _uow = uow;
    }

    public async Task<ApiResponse<long>> Handle(ImportOpeningBalancesCommand cmd, CancellationToken ct)
    {
        // One active opening voucher at a time — reverse the old one to re-import.
        var existingIds = await _journalRepo.Query().AsNoTracking()
            .Where(j => j.SourceType == "OpeningBalance" && j.Status == JournalEntryStatus.Posted
                     && j.ReversedEntryId == null)
            .Select(j => j.Id)
            .ToListAsync(ct);
        foreach (var id in existingIds)
        {
            var reversed = await _journalRepo.AnyAsync(j => j.ReversedEntryId == id, ct);
            if (!reversed)
                return ApiResponse<long>.Fail(
                    "An opening-balance voucher already exists — reverse it first to re-import.");
        }

        var refusal = await _periodGuard.CheckAsync(cmd.AsOfDate, isManualVoucher: true, ct);
        if (refusal is not null) return ApiResponse<long>.Fail(refusal);

        var accountIds = cmd.Lines.Select(l => l.AccountId).Distinct().ToList();
        var accounts = await _accountRepo.Query()
            .Where(a => accountIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, ct);
        foreach (var id in accountIds)
        {
            if (!accounts.TryGetValue(id, out var acc))
                return ApiResponse<long>.Fail($"Account {id} not found.");
            if (acc.IsGroup) return ApiResponse<long>.Fail($"'{acc.Name}' is a group account — openings go to detail accounts.");
            if (!acc.IsActive) return ApiResponse<long>.Fail($"'{acc.Name}' is inactive.");
        }

        var equity = await _accountRepo.Query()
            .FirstOrDefaultAsync(a => a.Code == Accounting.LedgerAccounts.OpeningBalanceEquity, ct);
        if (equity is null)
            return ApiResponse<long>.Fail("Opening Balance Equity account (3150) not found.");

        var effective = cmd.Lines.Where(l => l.Debit > 0 || l.Credit > 0).ToList();
        var totalDebit = effective.Sum(l => Math.Round(l.Debit, 2, MidpointRounding.AwayFromZero));
        var totalCredit = effective.Sum(l => Math.Round(l.Credit, 2, MidpointRounding.AwayFromZero));
        var plug = totalDebit - totalCredit;   // >0 → credit 3150; <0 → debit 3150

        var lines = effective.Select((l, i) => new JournalEntryLine
        {
            AccountId = l.AccountId,
            Debit = Math.Round(l.Debit, 2, MidpointRounding.AwayFromZero),
            Credit = Math.Round(l.Credit, 2, MidpointRounding.AwayFromZero),
            SortOrder = i
        }).ToList();

        if (plug != 0m)
        {
            lines.Add(new JournalEntryLine
            {
                AccountId = equity.Id,
                Debit = plug < 0 ? -plug : 0m,
                Credit = plug > 0 ? plug : 0m,
                LineNarration = "Opening balance equity (auto plug)",
                SortOrder = lines.Count
            });
        }

        var entry = new JournalEntry
        {
            Code = await _numbering.NextAsync("OB", null, ct),
            EntryDate = cmd.AsOfDate,
            Narration = $"Ledger opening balances as of {cmd.AsOfDate:yyyy-MM-dd}",
            Status = JournalEntryStatus.Posted,
            VoucherType = VoucherType.Opening,
            AccountingPeriodId = await _periodGuard.GetPeriodIdAsync(cmd.AsOfDate, ct),
            SourceType = "OpeningBalance",
            SourceId = 0,
            SourceCode = "OPENING",
            PostedAt = DateTimeOffset.UtcNow,
            PostedBy = _currentUser.UserName ?? "system",
            Lines = lines
        };

        await _journalRepo.AddAsync(entry, ct);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse<long>.Ok(entry.Id,
            plug == 0m
                ? $"Opening voucher {entry.Code} posted (balanced)."
                : $"Opening voucher {entry.Code} posted — {Math.Abs(plug):N2} plugged to Opening Balance Equity.");
    }
}
