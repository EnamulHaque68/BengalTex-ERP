using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Accounting.Revaluation;

// ═══════════════════════════ DTOs ═══════════════════════════

public sealed record FxRevaluationRowDto(
    string Kind,            // "AR" | "AP"
    string InvoiceCode,
    string CurrencyCode,
    decimal OutstandingFc,
    decimal BookedRate,
    decimal CurrentRate,
    decimal DeltaBdt);      // OutstandingFc × (CurrentRate − BookedRate); + = worth more

public sealed record FxRevaluationPreviewDto(
    DateOnly AsOfDate,
    IReadOnlyList<FxRevaluationRowDto> Rows,
    decimal ArDelta,        // + = receivables worth more (unrealized gain)
    decimal ApDelta,        // + = payables worth more (unrealized loss)
    decimal NetUnrealized); // ArDelta − ApDelta (net effect on P&L)

// ═══════════════════════════ Shared calc ═══════════════════════════

internal static class FxRevaluationCalc
{
    public static async Task<FxRevaluationPreviewDto> PreviewAsync(
        IRepository<Domain.Entities.CustomerInvoice, long> arRepo,
        IRepository<Domain.Entities.SupplierInvoice, long> apRepo,
        IExchangeRateResolver rates,
        DateOnly asOf, CancellationToken ct)
    {
        var rows = new List<FxRevaluationRowDto>();
        decimal arDelta = 0m, apDelta = 0m;

        var arInvoices = await arRepo.Query().AsNoTracking()
            .Where(i => (i.Status == CustomerInvoiceStatus.Issued || i.Status == CustomerInvoiceStatus.PartiallyPaid)
                     && !i.Currency.IsBaseCurrency && i.InvoiceDate <= asOf && i.TotalAmount - i.AmountPaid > 0m)
            .Select(i => new { i.Code, i.CurrencyId, CurrencyCode = i.Currency.Code, i.ExchangeRate, Outstanding = i.TotalAmount - i.AmountPaid })
            .ToListAsync(ct);
        foreach (var i in arInvoices)
        {
            var cur = await rates.GetRateAsOfAsync(i.CurrencyId, asOf, ct);
            var delta = Math.Round(i.Outstanding * (cur - i.ExchangeRate), 2, MidpointRounding.AwayFromZero);
            if (delta == 0m) continue;
            arDelta += delta;
            rows.Add(new FxRevaluationRowDto("AR", i.Code, i.CurrencyCode, i.Outstanding, i.ExchangeRate, cur, delta));
        }

        var apInvoices = await apRepo.Query().AsNoTracking()
            .Where(i => (i.Status == SupplierInvoiceStatus.Approved || i.Status == SupplierInvoiceStatus.PartiallyPaid)
                     && !i.Currency.IsBaseCurrency && i.InvoiceDate <= asOf && i.TotalAmount - i.AmountPaid > 0m)
            .Select(i => new { i.Code, i.CurrencyId, CurrencyCode = i.Currency.Code, i.ExchangeRate, Outstanding = i.TotalAmount - i.AmountPaid })
            .ToListAsync(ct);
        foreach (var i in apInvoices)
        {
            var cur = await rates.GetRateAsOfAsync(i.CurrencyId, asOf, ct);
            var delta = Math.Round(i.Outstanding * (cur - i.ExchangeRate), 2, MidpointRounding.AwayFromZero);
            if (delta == 0m) continue;
            apDelta += delta;
            rows.Add(new FxRevaluationRowDto("AP", i.Code, i.CurrencyCode, i.Outstanding, i.ExchangeRate, cur, delta));
        }

        arDelta = Math.Round(arDelta, 2);
        apDelta = Math.Round(apDelta, 2);
        return new FxRevaluationPreviewDto(asOf, rows, arDelta, apDelta, Math.Round(arDelta - apDelta, 2));
    }
}

// ═══════════════════════════ Preview ═══════════════════════════

public sealed record GetFxRevaluationPreviewQuery(DateOnly AsOfDate) : IRequest<ApiResponse<FxRevaluationPreviewDto>>;

internal sealed class GetFxRevaluationPreviewQueryHandler
    : IRequestHandler<GetFxRevaluationPreviewQuery, ApiResponse<FxRevaluationPreviewDto>>
{
    private readonly IRepository<Domain.Entities.CustomerInvoice, long> _arRepo;
    private readonly IRepository<Domain.Entities.SupplierInvoice, long> _apRepo;
    private readonly IExchangeRateResolver _rates;

    public GetFxRevaluationPreviewQueryHandler(
        IRepository<Domain.Entities.CustomerInvoice, long> arRepo, IRepository<Domain.Entities.SupplierInvoice, long> apRepo, IExchangeRateResolver rates)
    {
        _arRepo = arRepo; _apRepo = apRepo; _rates = rates;
    }

    public async Task<ApiResponse<FxRevaluationPreviewDto>> Handle(GetFxRevaluationPreviewQuery q, CancellationToken ct)
        => ApiResponse<FxRevaluationPreviewDto>.Ok(await FxRevaluationCalc.PreviewAsync(_arRepo, _apRepo, _rates, q.AsOfDate, ct));
}

// ═══════════════════════════ Post (auto-reversing) ═══════════════════════════

/// <summary>
/// Phase A7b (C9) — month-end foreign-currency revaluation. Restates open FC receivables/payables
/// at the as-of dated rate and books the unrealized difference to Unrealized Exchange Gain (4310) /
/// Loss (5810), with an auto-reversing entry the next day — so the Balance Sheet shows FC exposure
/// at month-end rates without double-counting the realized FX recognised at settlement.
/// </summary>
public sealed record PostFxRevaluationCommand(DateOnly AsOfDate) : IRequest<ApiResponse<long>>;

internal sealed class PostFxRevaluationCommandHandler : IRequestHandler<PostFxRevaluationCommand, ApiResponse<long>>
{
    private readonly IRepository<Domain.Entities.CustomerInvoice, long> _arRepo;
    private readonly IRepository<Domain.Entities.SupplierInvoice, long> _apRepo;
    private readonly IExchangeRateResolver _rates;
    private readonly IRepository<JournalEntry, long> _journalRepo;
    private readonly IRepository<Domain.Entities.Account> _accountRepo;
    private readonly IPeriodGuard _periodGuard;
    private readonly INumberingService _numbering;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public PostFxRevaluationCommandHandler(
        IRepository<Domain.Entities.CustomerInvoice, long> arRepo, IRepository<Domain.Entities.SupplierInvoice, long> apRepo, IExchangeRateResolver rates,
        IRepository<JournalEntry, long> journalRepo, IRepository<Domain.Entities.Account> accountRepo,
        IPeriodGuard periodGuard, INumberingService numbering, ICurrentUserService currentUser, IUnitOfWork uow)
    {
        _arRepo = arRepo; _apRepo = apRepo; _rates = rates; _journalRepo = journalRepo; _accountRepo = accountRepo;
        _periodGuard = periodGuard; _numbering = numbering; _currentUser = currentUser; _uow = uow;
    }

    public async Task<ApiResponse<long>> Handle(PostFxRevaluationCommand cmd, CancellationToken ct)
    {
        var refusal = await _periodGuard.CheckAsync(cmd.AsOfDate, isManualVoucher: true, ct);
        if (refusal is not null) return ApiResponse<long>.Fail(refusal);

        var p = await FxRevaluationCalc.PreviewAsync(_arRepo, _apRepo, _rates, cmd.AsOfDate, ct);
        if (p.ArDelta == 0m && p.ApDelta == 0m)
            return ApiResponse<long>.Fail("No open foreign-currency balances to revalue at this date.");

        var codes = new[]
        {
            Accounting.LedgerAccounts.AccountsReceivable, Accounting.LedgerAccounts.AccountsPayable,
            Accounting.LedgerAccounts.UnrealizedExchangeGain, Accounting.LedgerAccounts.UnrealizedExchangeLoss
        };
        var acc = await _accountRepo.Query().Where(a => codes.Contains(a.Code)).ToDictionaryAsync(a => a.Code, ct);
        foreach (var c in codes)
            if (!acc.ContainsKey(c)) return ApiResponse<long>.Fail($"Account {c} not found in the chart of accounts.");

        int Ar = acc[Accounting.LedgerAccounts.AccountsReceivable].Id;
        int Ap = acc[Accounting.LedgerAccounts.AccountsPayable].Id;
        int Gain = acc[Accounting.LedgerAccounts.UnrealizedExchangeGain].Id;
        int Loss = acc[Accounting.LedgerAccounts.UnrealizedExchangeLoss].Id;

        // Per-account net movement (ar: + = Dr AR; ap: + = Cr AP i.e. liability up).
        var ar = p.ArDelta;
        var ap = p.ApDelta;
        var gain = (ar > 0 ? ar : 0m) + (ap < 0 ? -ap : 0m);   // Cr 4310
        var loss = (ar < 0 ? -ar : 0m) + (ap > 0 ? ap : 0m);   // Dr 5810

        List<JournalEntryLine> BuildLines(bool reverse)
        {
            var lines = new List<JournalEntryLine>();
            var sort = 0;
            void Add(int accountId, decimal dr, decimal cr)
            {
                if (dr == 0m && cr == 0m) return;
                lines.Add(new JournalEntryLine { AccountId = accountId, Debit = reverse ? cr : dr, Credit = reverse ? dr : cr, SortOrder = sort++ });
            }
            Add(Ar, ar > 0 ? ar : 0m, ar < 0 ? -ar : 0m);
            Add(Ap, ap < 0 ? -ap : 0m, ap > 0 ? ap : 0m);
            Add(Gain, 0m, gain);
            Add(Loss, loss, 0m);
            return lines;
        }

        var snapshot = new JournalEntry
        {
            Code = await _numbering.NextAsync("JV", null, ct),
            EntryDate = cmd.AsOfDate,
            Narration = $"FX revaluation as of {cmd.AsOfDate:yyyy-MM-dd} — open FC AR/AP restated to month-end rates",
            Status = JournalEntryStatus.Posted, VoucherType = VoucherType.Journal,
            AccountingPeriodId = await _periodGuard.GetPeriodIdAsync(cmd.AsOfDate, ct),
            SourceType = "FxRevaluation", SourceId = 0, SourceCode = "FX-REVAL",
            PostedAt = DateTimeOffset.UtcNow, PostedBy = _currentUser.UserName ?? "system",
            Lines = BuildLines(reverse: false)
        };
        await _journalRepo.AddAsync(snapshot, ct);

        var revDate = cmd.AsOfDate.AddDays(1);
        var reversal = new JournalEntry
        {
            Code = await _numbering.NextAsync("JV", null, ct),
            EntryDate = revDate,
            Narration = $"FX revaluation reversal ({cmd.AsOfDate:yyyy-MM-dd})",
            Status = JournalEntryStatus.Posted, VoucherType = VoucherType.Journal,
            AccountingPeriodId = await _periodGuard.GetPeriodIdAsync(revDate, ct),
            SourceType = "FxRevaluationReversal", SourceId = 0, SourceCode = "FX-REVAL-REV",
            PostedAt = DateTimeOffset.UtcNow, PostedBy = _currentUser.UserName ?? "system",
            Lines = BuildLines(reverse: true)
        };
        await _journalRepo.AddAsync(reversal, ct);
        await _uow.SaveChangesAsync(ct);

        return ApiResponse<long>.Ok(snapshot.Id,
            $"FX revaluation posted — net unrealized {p.NetUnrealized:N2} (AR {p.ArDelta:N2}, AP {p.ApDelta:N2}); auto-reverses {revDate:yyyy-MM-dd}.");
    }
}
