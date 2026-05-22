using BengalTex.ERP.Application.Accounting.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Accounting.Queries;

/// <summary>Trial balance from POSTED journal lines up to (and including) the as-of date.</summary>
public sealed record GetTrialBalanceQuery(DateOnly? AsOfDate = null)
    : IRequest<ApiResponse<TrialBalanceDto>>;

internal sealed class GetTrialBalanceQueryHandler
    : IRequestHandler<GetTrialBalanceQuery, ApiResponse<TrialBalanceDto>>
{
    private readonly IRepository<JournalEntryLine, long> _lineRepo;
    private readonly IRepository<Domain.Entities.Account> _accountRepo;

    public GetTrialBalanceQueryHandler(
        IRepository<JournalEntryLine, long> lineRepo,
        IRepository<Domain.Entities.Account> accountRepo)
    {
        _lineRepo = lineRepo;
        _accountRepo = accountRepo;
    }

    public async Task<ApiResponse<TrialBalanceDto>> Handle(
        GetTrialBalanceQuery request, CancellationToken cancellationToken)
    {
        var asOf = request.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        // Per-account posted totals up to the date (flat aggregate — safe to translate).
        var totals = await _lineRepo.Query()
            .Where(l => l.JournalEntry.Status == JournalEntryStatus.Posted
                     && l.JournalEntry.EntryDate <= asOf)
            .GroupBy(l => l.AccountId)
            .Select(g => new { AccountId = g.Key, Debit = g.Sum(x => x.Debit), Credit = g.Sum(x => x.Credit) })
            .ToListAsync(cancellationToken);

        if (totals.Count == 0)
            return ApiResponse<TrialBalanceDto>.Ok(new TrialBalanceDto(asOf, new List<TrialBalanceRowDto>(), 0, 0, true));

        var accountIds = totals.Select(t => t.AccountId).ToList();
        var accounts = await _accountRepo.Query()
            .Where(a => accountIds.Contains(a.Id))
            .Select(a => new { a.Id, a.Code, a.Name, a.AccountType })
            .ToListAsync(cancellationToken);
        var accById = accounts.ToDictionary(a => a.Id);

        var rows = new List<TrialBalanceRowDto>();
        foreach (var t in totals)
        {
            if (!accById.TryGetValue(t.AccountId, out var acc)) continue;
            var net = t.Debit - t.Credit;                 // + → debit side, − → credit side
            var debitBal = net > 0 ? net : 0m;
            var creditBal = net < 0 ? -net : 0m;
            if (debitBal == 0m && creditBal == 0m) continue;   // skip nil-balance accounts
            rows.Add(new TrialBalanceRowDto(
                acc.Id, acc.Code, acc.Name, acc.AccountType.ToString(), debitBal, creditBal));
        }

        rows = rows.OrderBy(r => r.AccountCode).ToList();
        var totalDebit = rows.Sum(r => r.DebitBalance);
        var totalCredit = rows.Sum(r => r.CreditBalance);

        return ApiResponse<TrialBalanceDto>.Ok(new TrialBalanceDto(
            asOf, rows, totalDebit, totalCredit, totalDebit == totalCredit));
    }
}
