using BengalTex.ERP.Application.Accounting.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Accounting.Queries;

/// <summary>
/// Balance Sheet as of a date from POSTED journal lines. Equity is shown together with the
/// computed current earnings (cumulative Income − Expense up to the date) so the sheet balances:
/// Assets = Liabilities + Equity + Current Earnings.
/// </summary>
public sealed record GetBalanceSheetQuery(DateOnly? AsOfDate = null)
    : IRequest<ApiResponse<BalanceSheetDto>>;

internal sealed class GetBalanceSheetQueryHandler
    : IRequestHandler<GetBalanceSheetQuery, ApiResponse<BalanceSheetDto>>
{
    private readonly IRepository<JournalEntryLine, long> _lineRepo;
    private readonly IRepository<Domain.Entities.Account> _accountRepo;

    public GetBalanceSheetQueryHandler(
        IRepository<JournalEntryLine, long> lineRepo,
        IRepository<Domain.Entities.Account> accountRepo)
    {
        _lineRepo = lineRepo;
        _accountRepo = accountRepo;
    }

    public async Task<ApiResponse<BalanceSheetDto>> Handle(
        GetBalanceSheetQuery request, CancellationToken cancellationToken)
    {
        var asOf = request.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var totals = await _lineRepo.Query()
            .Where(l => l.JournalEntry.Status == JournalEntryStatus.Posted
                     && l.JournalEntry.EntryDate <= asOf)
            .GroupBy(l => l.AccountId)
            .Select(g => new { AccountId = g.Key, Debit = g.Sum(x => x.Debit), Credit = g.Sum(x => x.Credit) })
            .ToListAsync(cancellationToken);

        var accounts = await _accountRepo.Query()
            .Select(a => new { a.Id, a.Code, a.Name, a.AccountType })
            .ToListAsync(cancellationToken);
        var accById = accounts.ToDictionary(a => a.Id);

        var assets = new List<StatementLineDto>();
        var liabilities = new List<StatementLineDto>();
        var equity = new List<StatementLineDto>();
        decimal income = 0m, expense = 0m;

        foreach (var t in totals)
        {
            if (!accById.TryGetValue(t.AccountId, out var acc)) continue;
            switch (acc.AccountType)
            {
                case AccountType.Asset:
                    var aAmt = t.Debit - t.Credit;
                    if (aAmt != 0m) assets.Add(new StatementLineDto(acc.Id, acc.Code, acc.Name, aAmt));
                    break;
                case AccountType.Liability:
                    var lAmt = t.Credit - t.Debit;
                    if (lAmt != 0m) liabilities.Add(new StatementLineDto(acc.Id, acc.Code, acc.Name, lAmt));
                    break;
                case AccountType.Equity:
                    var eAmt = t.Credit - t.Debit;
                    if (eAmt != 0m) equity.Add(new StatementLineDto(acc.Id, acc.Code, acc.Name, eAmt));
                    break;
                case AccountType.Income:
                    income += t.Credit - t.Debit;
                    break;
                case AccountType.Expense:
                    expense += t.Debit - t.Credit;
                    break;
            }
        }

        assets = assets.OrderBy(x => x.AccountCode).ToList();
        liabilities = liabilities.OrderBy(x => x.AccountCode).ToList();
        equity = equity.OrderBy(x => x.AccountCode).ToList();

        var totalAssets = assets.Sum(x => x.Amount);
        var totalLiabilities = liabilities.Sum(x => x.Amount);
        var currentEarnings = income - expense;
        var totalEquity = equity.Sum(x => x.Amount) + currentEarnings;
        var totalLiabEquity = totalLiabilities + totalEquity;

        return ApiResponse<BalanceSheetDto>.Ok(new BalanceSheetDto(
            asOf, assets, totalAssets, liabilities, totalLiabilities,
            equity, currentEarnings, totalEquity, totalLiabEquity,
            Math.Abs(totalAssets - totalLiabEquity) < 0.005m));
    }
}
