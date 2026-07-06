using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Infrastructure.Services;

/// <summary>
/// Builds an auto-generated POSTED <see cref="JournalEntry"/> from source-document lines.
/// Resolves accounts by Code; rounds amounts to 2 dp; drops nil lines; refuses to post an
/// unbalanced set (guards against a wiring bug). Does NOT SaveChanges (caller commits).
///
/// Phase A1: enforces the fiscal-period guard (a locked period rejects the posting with a 400
/// validation error), classifies the entry's <see cref="VoucherType"/> from its source type
/// (Receipt→RV, Payment/Expense→PV, OpeningBalance→OB, YearEndClose→CL, else JV — each with
/// its own numbering series) and stamps the covering <c>AccountingPeriodId</c>.
/// </summary>
public sealed class JournalPostingService : IJournalPostingService
{
    private readonly IRepository<JournalEntry, long> _journalRepo;
    private readonly IRepository<Account> _accountRepo;
    private readonly INumberingService _numbering;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IPeriodGuard _periodGuard;

    public JournalPostingService(
        IRepository<JournalEntry, long> journalRepo,
        IRepository<Account> accountRepo,
        INumberingService numbering,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IPeriodGuard periodGuard)
    {
        _journalRepo = journalRepo;
        _accountRepo = accountRepo;
        _numbering = numbering;
        _currentUser = currentUser;
        _clock = clock;
        _periodGuard = periodGuard;
    }

    /// <summary>Voucher classification by originating document type (Phase A1 taxonomy).</summary>
    internal static VoucherType ClassifyVoucher(string sourceType) => sourceType switch
    {
        "Receipt" => VoucherType.Receipt,
        "Payment" => VoucherType.Payment,
        "Expense" => VoucherType.Payment,
        "OpeningBalance" => VoucherType.Opening,
        "YearEndClose" => VoucherType.Closing,
        _ => VoucherType.Journal
    };

    /// <summary>Numbering series per voucher type (JV/RV/PV/CV/OB/CL).</summary>
    internal static string SeriesFor(VoucherType type) => type switch
    {
        VoucherType.Receipt => "RV",
        VoucherType.Payment => "PV",
        VoucherType.Contra => "CV",
        VoucherType.Opening => "OB",
        VoucherType.Closing => "CL",
        _ => "JV"
    };

    public async Task PostAsync(
        DateOnly date, string narration, string sourceType, long sourceId, string sourceCode,
        IReadOnlyList<JournalPostingLine> lines, CancellationToken ct = default)
    {
        var effective = lines
            .Select(l => new JournalPostingLine(l.AccountCode, Math.Round(l.Debit, 2, MidpointRounding.AwayFromZero), Math.Round(l.Credit, 2, MidpointRounding.AwayFromZero), l.Dims))
            .Where(l => l.Debit != 0m || l.Credit != 0m)
            .ToList();
        if (effective.Count == 0) return;   // nothing to post (e.g. zero-value document)

        // ── Phase A1: fiscal-period guard (auto-journal path). Year-end close is exempt —
        //    it must post into its own (already locked) year by design.
        var voucherType = ClassifyVoucher(sourceType);
        if (voucherType != VoucherType.Closing)
        {
            var refusal = await _periodGuard.CheckAsync(date, isManualVoucher: false, ct);
            if (refusal is not null)
                throw new ValidationException(new[] { new ValidationFailure("EntryDate", refusal) });
        }

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

        // Phase A3 — enforce cost center on accounts flagged RequiresCostCenter.
        foreach (var l in effective)
        {
            var acc = accounts[l.AccountCode];
            if (acc.RequiresCostCenter && l.Dims?.CostCenterId is null)
                throw new FluentValidation.ValidationException(new[]
                {
                    new FluentValidation.Results.ValidationFailure("CostCenter",
                        $"Account {acc.Code} — {acc.Name} requires a cost center.")
                });
        }

        var now = _clock.UtcNow;
        var entry = new JournalEntry
        {
            Code = await _numbering.NextAsync(SeriesFor(voucherType), null, ct),
            EntryDate = date,
            Narration = narration,
            Status = JournalEntryStatus.Posted,
            VoucherType = voucherType,
            AccountingPeriodId = await _periodGuard.GetPeriodIdAsync(date, ct),
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
                SortOrder = i,
                // Phase A3 — stamp dimensions
                CostCenterId = l.Dims?.CostCenterId,
                BuyerId = l.Dims?.BuyerId,
                StyleId = l.Dims?.StyleId,
                SalesOrderId = l.Dims?.SalesOrderId,
                ProductionOrderId = l.Dims?.ProductionOrderId
            }).ToList()
        };

        await _journalRepo.AddAsync(entry, ct);   // caller commits
    }
}
