using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Accounting.InventoryGL;

// ═══════════════════════════ Absorption true-up (M6) ═══════════════════════════

public sealed record AbsorptionTrueUpPreviewDto(
    decimal AppliedLabour, decimal AppliedFactoryOverhead, decimal AppliedTotal,
    decimal ActualLabour, decimal ActualFactoryOverhead, decimal ActualTotal,
    decimal Variance, string VarianceKind);   // Variance = Actual − Applied; "Under-absorbed" (extra cost) / "Over-absorbed"

public sealed record GetAbsorptionTrueUpPreviewQuery(DateOnly FromDate, DateOnly ToDate)
    : IRequest<ApiResponse<AbsorptionTrueUpPreviewDto>>;

internal sealed class GetAbsorptionTrueUpPreviewQueryHandler
    : IRequestHandler<GetAbsorptionTrueUpPreviewQuery, ApiResponse<AbsorptionTrueUpPreviewDto>>
{
    private readonly IRepository<JournalEntryLine, long> _lineRepo;
    public GetAbsorptionTrueUpPreviewQueryHandler(IRepository<JournalEntryLine, long> lineRepo) => _lineRepo = lineRepo;

    public async Task<ApiResponse<AbsorptionTrueUpPreviewDto>> Handle(GetAbsorptionTrueUpPreviewQuery q, CancellationToken ct)
        => ApiResponse<AbsorptionTrueUpPreviewDto>.Ok(await AbsorptionCalc.PreviewAsync(_lineRepo, q.FromDate, q.ToDate, ct));
}

internal static class AbsorptionCalc
{
    /// <summary>Applied contra (Cr) vs actual pool (Dr) balances for a period.</summary>
    public static async Task<AbsorptionTrueUpPreviewDto> PreviewAsync(
        IRepository<JournalEntryLine, long> lineRepo, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var codes = new[]
        {
            Accounting.LedgerAccounts.AppliedLabour, Accounting.LedgerAccounts.AppliedFactoryOverhead,
            Accounting.LedgerAccounts.SalaryExpense, Accounting.LedgerAccounts.FactoryOverhead
        };
        var bal = await lineRepo.Query().AsNoTracking()
            .Where(l => l.JournalEntry.Status == JournalEntryStatus.Posted
                     && l.JournalEntry.VoucherType != VoucherType.Closing
                     && l.JournalEntry.EntryDate >= from && l.JournalEntry.EntryDate <= to
                     && codes.Contains(l.Account.Code))
            .GroupBy(l => l.Account.Code)
            .Select(g => new { Code = g.Key, Debit = g.Sum(x => x.Debit), Credit = g.Sum(x => x.Credit) })
            .ToListAsync(ct);

        decimal Applied(string code) { var r = bal.FirstOrDefault(x => x.Code == code); return r is null ? 0m : r.Credit - r.Debit; }
        decimal Actual(string code) { var r = bal.FirstOrDefault(x => x.Code == code); return r is null ? 0m : r.Debit - r.Credit; }

        var appLab = Math.Round(Applied(Accounting.LedgerAccounts.AppliedLabour), 2);
        var appFoh = Math.Round(Applied(Accounting.LedgerAccounts.AppliedFactoryOverhead), 2);
        var actLab = Math.Round(Actual(Accounting.LedgerAccounts.SalaryExpense), 2);
        var actFoh = Math.Round(Actual(Accounting.LedgerAccounts.FactoryOverhead), 2);
        var variance = (actLab + actFoh) - (appLab + appFoh);

        return new AbsorptionTrueUpPreviewDto(
            appLab, appFoh, appLab + appFoh, actLab, actFoh, actLab + actFoh,
            Math.Round(variance, 2), variance >= 0 ? "Under-absorbed" : "Over-absorbed");
    }
}

/// <summary>
/// Phase A4 (M6) — month-end absorption true-up. Relieves the applied contra accounts (5211,
/// 5311) into Over/Under-Absorbed Overhead (5165): Dr Applied Labour + Dr Applied FOH / Cr 5165.
/// This zeroes the temporary applied accounts each period; combined with the actual pools
/// (5200/5300 debits) the P&amp;L nets to actual − absorbed = the true production cost recognised.
/// </summary>
public sealed record PostAbsorptionTrueUpCommand(DateOnly FromDate, DateOnly ToDate, DateOnly PostDate)
    : IRequest<ApiResponse<long>>;

internal sealed class PostAbsorptionTrueUpCommandHandler : IRequestHandler<PostAbsorptionTrueUpCommand, ApiResponse<long>>
{
    private readonly IRepository<JournalEntryLine, long> _lineRepo;
    private readonly IRepository<JournalEntry, long> _journalRepo;
    private readonly IRepository<Domain.Entities.Account> _accountRepo;
    private readonly IPeriodGuard _periodGuard;
    private readonly INumberingService _numbering;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public PostAbsorptionTrueUpCommandHandler(
        IRepository<JournalEntryLine, long> lineRepo, IRepository<JournalEntry, long> journalRepo,
        IRepository<Domain.Entities.Account> accountRepo, IPeriodGuard periodGuard,
        INumberingService numbering, ICurrentUserService currentUser, IUnitOfWork uow)
    {
        _lineRepo = lineRepo; _journalRepo = journalRepo; _accountRepo = accountRepo;
        _periodGuard = periodGuard; _numbering = numbering; _currentUser = currentUser; _uow = uow;
    }

    public async Task<ApiResponse<long>> Handle(PostAbsorptionTrueUpCommand cmd, CancellationToken ct)
    {
        var refusal = await _periodGuard.CheckAsync(cmd.PostDate, isManualVoucher: true, ct);
        if (refusal is not null) return ApiResponse<long>.Fail(refusal);

        var p = await AbsorptionCalc.PreviewAsync(_lineRepo, cmd.FromDate, cmd.ToDate, ct);
        if (p.AppliedTotal == 0m)
            return ApiResponse<long>.Fail("No applied conversion cost in this period — nothing to true up.");

        var applied = await _accountRepo.Query()
            .Where(a => a.Code == Accounting.LedgerAccounts.AppliedLabour
                     || a.Code == Accounting.LedgerAccounts.AppliedFactoryOverhead
                     || a.Code == Accounting.LedgerAccounts.OverheadAbsorptionVariance)
            .ToDictionaryAsync(a => a.Code, ct);

        var lines = new List<JournalEntryLine>();
        var sort = 0;
        if (p.AppliedLabour != 0m)
            lines.Add(new JournalEntryLine { AccountId = applied[Accounting.LedgerAccounts.AppliedLabour].Id, Debit = p.AppliedLabour, Credit = 0m, SortOrder = sort++ });
        if (p.AppliedFactoryOverhead != 0m)
            lines.Add(new JournalEntryLine { AccountId = applied[Accounting.LedgerAccounts.AppliedFactoryOverhead].Id, Debit = p.AppliedFactoryOverhead, Credit = 0m, SortOrder = sort++ });
        lines.Add(new JournalEntryLine { AccountId = applied[Accounting.LedgerAccounts.OverheadAbsorptionVariance].Id, Debit = 0m, Credit = p.AppliedTotal, SortOrder = sort });

        var entry = new JournalEntry
        {
            Code = await _numbering.NextAsync("JV", null, ct),
            EntryDate = cmd.PostDate,
            Narration = $"Absorption true-up {cmd.FromDate:yyyy-MM-dd}..{cmd.ToDate:yyyy-MM-dd} — applied conversion relieved to 5165",
            Status = JournalEntryStatus.Posted,
            VoucherType = VoucherType.Journal,
            AccountingPeriodId = await _periodGuard.GetPeriodIdAsync(cmd.PostDate, ct),
            SourceType = "AbsorptionTrueUp",
            SourceId = 0,
            SourceCode = "ABS-TRUEUP",
            PostedAt = DateTimeOffset.UtcNow,
            PostedBy = _currentUser.UserName ?? "system",
            Lines = lines
        };
        await _journalRepo.AddAsync(entry, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<long>.Ok(entry.Id,
            $"Absorption trued up — {p.AppliedTotal:N2} applied relieved; period variance (actual − applied) {p.Variance:N2} ({p.VarianceKind}).");
    }
}

// ═══════════════════════════ WIP snapshot (M7) ═══════════════════════════

/// <summary>
/// Phase A4 (M7) — month-end WIP snapshot. Values in-progress production orders at cut-off
/// (Quantity × the output product's WAC as a proxy) and posts Dr WIP / Cr WIP Accrual (2155),
/// with an auto-reversing entry dated the next day — so the Balance Sheet shows real WIP at
/// month-end without double-counting once the order completes. Interim fix for F8.
/// </summary>
public sealed record PostWipSnapshotCommand(DateOnly AsOfDate) : IRequest<ApiResponse<long>>;

internal sealed class PostWipSnapshotCommandHandler : IRequestHandler<PostWipSnapshotCommand, ApiResponse<long>>
{
    private readonly IRepository<Domain.Entities.ProductionOrder, long> _poRepo;
    private readonly IRepository<JournalEntry, long> _journalRepo;
    private readonly IRepository<Domain.Entities.Account> _accountRepo;
    private readonly IPeriodGuard _periodGuard;
    private readonly INumberingService _numbering;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public PostWipSnapshotCommandHandler(
        IRepository<Domain.Entities.ProductionOrder, long> poRepo, IRepository<JournalEntry, long> journalRepo,
        IRepository<Domain.Entities.Account> accountRepo, IPeriodGuard periodGuard,
        INumberingService numbering, ICurrentUserService currentUser, IUnitOfWork uow)
    {
        _poRepo = poRepo; _journalRepo = journalRepo; _accountRepo = accountRepo;
        _periodGuard = periodGuard; _numbering = numbering; _currentUser = currentUser; _uow = uow;
    }

    public async Task<ApiResponse<long>> Handle(PostWipSnapshotCommand cmd, CancellationToken ct)
    {
        var refusal = await _periodGuard.CheckAsync(cmd.AsOfDate, isManualVoucher: true, ct);
        if (refusal is not null) return ApiResponse<long>.Fail(refusal);

        var wipValue = await _poRepo.Query().AsNoTracking()
            .Where(p => p.Status == ProductionOrderStatus.InProgress)
            .Select(p => p.Quantity * p.Product.WeightedAverageCost)
            .SumAsync(ct);
        wipValue = Math.Round(wipValue, 2);
        if (wipValue <= 0m) return ApiResponse<long>.Fail("No in-progress production orders to snapshot.");

        var wip = await _accountRepo.Query().FirstOrDefaultAsync(a => a.Code == Accounting.LedgerAccounts.WorkInProgressInventory, ct);
        var accrual = await _accountRepo.Query().FirstOrDefaultAsync(a => a.Code == Accounting.LedgerAccounts.WipAccrual, ct);
        if (wip is null || accrual is null) return ApiResponse<long>.Fail("WIP (1160) or WIP Accrual (2155) account not found.");

        var periodId = await _periodGuard.GetPeriodIdAsync(cmd.AsOfDate, ct);

        // Snapshot (as-of) — Dr WIP / Cr WIP Accrual.
        var snapshot = new JournalEntry
        {
            Code = await _numbering.NextAsync("JV", null, ct),
            EntryDate = cmd.AsOfDate,
            Narration = $"WIP snapshot as of {cmd.AsOfDate:yyyy-MM-dd} (in-progress production accrual)",
            Status = JournalEntryStatus.Posted, VoucherType = VoucherType.Journal, AccountingPeriodId = periodId,
            SourceType = "WipSnapshot", SourceId = 0, SourceCode = "WIP-SNAP",
            PostedAt = DateTimeOffset.UtcNow, PostedBy = _currentUser.UserName ?? "system",
            Lines =
            {
                new JournalEntryLine { AccountId = wip.Id, Debit = wipValue, Credit = 0m, SortOrder = 0 },
                new JournalEntryLine { AccountId = accrual.Id, Debit = 0m, Credit = wipValue, SortOrder = 1 }
            }
        };
        await _journalRepo.AddAsync(snapshot, ct);

        // Auto-reversal dated the next day.
        var revDate = cmd.AsOfDate.AddDays(1);
        var reversal = new JournalEntry
        {
            Code = await _numbering.NextAsync("JV", null, ct),
            EntryDate = revDate,
            Narration = $"WIP snapshot reversal ({cmd.AsOfDate:yyyy-MM-dd} accrual)",
            Status = JournalEntryStatus.Posted, VoucherType = VoucherType.Journal,
            AccountingPeriodId = await _periodGuard.GetPeriodIdAsync(revDate, ct),
            SourceType = "WipSnapshotReversal", SourceId = 0, SourceCode = "WIP-SNAP-REV",
            PostedAt = DateTimeOffset.UtcNow, PostedBy = _currentUser.UserName ?? "system",
            Lines =
            {
                new JournalEntryLine { AccountId = accrual.Id, Debit = wipValue, Credit = 0m, SortOrder = 0 },
                new JournalEntryLine { AccountId = wip.Id, Debit = 0m, Credit = wipValue, SortOrder = 1 }
            }
        };
        await _journalRepo.AddAsync(reversal, ct);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse<long>.Ok(snapshot.Id, $"WIP snapshot posted ({wipValue:N2}) — auto-reverses {revDate:yyyy-MM-dd}.");
    }
}
