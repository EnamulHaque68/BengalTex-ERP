using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Infrastructure.Services;

/// <summary>
/// Builds an auto-generated POSTED <see cref="JournalEntry"/> from source-document lines.
/// Resolves accounts by Code; rounds amounts to 2 dp; drops nil lines; refuses to post an
/// unbalanced set (guards against a wiring bug). Does NOT SaveChanges (caller commits).
/// </summary>
public sealed class JournalPostingService : IJournalPostingService
{
    private readonly IRepository<JournalEntry, long> _journalRepo;
    private readonly IRepository<Account> _accountRepo;
    private readonly INumberingService _numbering;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public JournalPostingService(
        IRepository<JournalEntry, long> journalRepo,
        IRepository<Account> accountRepo,
        INumberingService numbering,
        ICurrentUserService currentUser,
        IDateTimeProvider clock)
    {
        _journalRepo = journalRepo;
        _accountRepo = accountRepo;
        _numbering = numbering;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task PostAsync(
        DateOnly date, string narration, string sourceType, long sourceId, string sourceCode,
        IReadOnlyList<JournalPostingLine> lines, CancellationToken ct = default)
    {
        var effective = lines
            .Select(l => new JournalPostingLine(l.AccountCode, Math.Round(l.Debit, 2, MidpointRounding.AwayFromZero), Math.Round(l.Credit, 2, MidpointRounding.AwayFromZero)))
            .Where(l => l.Debit != 0m || l.Credit != 0m)
            .ToList();
        if (effective.Count == 0) return;   // nothing to post (e.g. zero-value document)

        var totalDebit = effective.Sum(l => l.Debit);
        var totalCredit = effective.Sum(l => l.Credit);
        if (Math.Abs(totalDebit - totalCredit) >= 0.01m)
            throw new InvalidOperationException(
                $"Auto-journal for {sourceType} {sourceCode} is unbalanced (Dr {totalDebit} vs Cr {totalCredit}).");

        var codes = effective.Select(l => l.AccountCode).Distinct().ToList();
        var accounts = await _accountRepo.Query()
            .Where(a => codes.Contains(a.Code))
            .ToDictionaryAsync(a => a.Code, ct);

        var missing = codes.Where(c => !accounts.ContainsKey(c)).ToList();
        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"Ledger account(s) {string.Join(", ", missing)} not found — chart of accounts not seeded?");

        var now = _clock.UtcNow;
        var entry = new JournalEntry
        {
            Code = await _numbering.NextAsync("JV", null, ct),
            EntryDate = date,
            Narration = narration,
            Status = JournalEntryStatus.Posted,
            SourceType = sourceType,
            SourceId = sourceId,
            SourceCode = sourceCode,
            PostedAt = now,
            PostedBy = _currentUser.UserName ?? "system",
            Lines = effective.Select((l, i) => new JournalEntryLine
            {
                AccountId = accounts[l.AccountCode].Id,
                Debit = l.Debit,
                Credit = l.Credit,
                SortOrder = i
            }).ToList()
        };

        await _journalRepo.AddAsync(entry, ct);   // caller commits
    }
}
