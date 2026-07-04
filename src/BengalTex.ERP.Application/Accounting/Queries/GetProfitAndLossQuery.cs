using BengalTex.ERP.Application.Accounting.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Accounting.Queries;

/// <summary>Profit &amp; Loss for a period from POSTED journal lines (income − expense).</summary>
public sealed record GetProfitAndLossQuery(DateOnly FromDate, DateOnly ToDate)
    : IRequest<ApiResponse<ProfitAndLossDto>>;

internal sealed class GetProfitAndLossQueryHandler
    : IRequestHandler<GetProfitAndLossQuery, ApiResponse<ProfitAndLossDto>>
{
    private readonly IRepository<JournalEntryLine, long> _lineRepo;
    private readonly IRepository<Domain.Entities.Account> _accountRepo;

    public GetProfitAndLossQueryHandler(
        IRepository<JournalEntryLine, long> lineRepo,
        IRepository<Domain.Entities.Account> accountRepo)
    {
        _lineRepo = lineRepo;
        _accountRepo = accountRepo;
    }

    public async Task<ApiResponse<ProfitAndLossDto>> Handle(
        GetProfitAndLossQuery request, CancellationToken cancellationToken)
    {
        var totals = await _lineRepo.Query()
            .Where(l => l.JournalEntry.Status == JournalEntryStatus.Posted
                     // Phase A1: year-end closing vouchers zero the P&L accounts into Retained
                     // Earnings — excluding them keeps a closed year's P&L historically correct.
                     && l.JournalEntry.VoucherType != VoucherType.Closing
                     && l.JournalEntry.EntryDate >= request.FromDate
                     && l.JournalEntry.EntryDate <= request.ToDate)
            .GroupBy(l => l.AccountId)
            .Select(g => new { AccountId = g.Key, Debit = g.Sum(x => x.Debit), Credit = g.Sum(x => x.Credit) })
            .ToListAsync(cancellationToken);

        var accounts = await _accountRepo.Query()
            .Where(a => a.AccountType == AccountType.Income || a.AccountType == AccountType.Expense)
            .Select(a => new { a.Id, a.Code, a.Name, a.AccountType })
            .ToListAsync(cancellationToken);
        var accById = accounts.ToDictionary(a => a.Id);

        var income = new List<StatementLineDto>();
        var expenses = new List<StatementLineDto>();
        foreach (var t in totals)
        {
            if (!accById.TryGetValue(t.AccountId, out var acc)) continue;
            if (acc.AccountType == AccountType.Income)
            {
                var amount = t.Credit - t.Debit;     // income is credit-normal
                if (amount != 0m) income.Add(new StatementLineDto(acc.Id, acc.Code, acc.Name, amount));
            }
            else // Expense
            {
                var amount = t.Debit - t.Credit;      // expense is debit-normal
                if (amount != 0m) expenses.Add(new StatementLineDto(acc.Id, acc.Code, acc.Name, amount));
            }
        }

        income = income.OrderBy(x => x.AccountCode).ToList();
        expenses = expenses.OrderBy(x => x.AccountCode).ToList();
        var totalIncome = income.Sum(x => x.Amount);
        var totalExpense = expenses.Sum(x => x.Amount);

        return ApiResponse<ProfitAndLossDto>.Ok(new ProfitAndLossDto(
            request.FromDate, request.ToDate, income, totalIncome, expenses, totalExpense,
            totalIncome - totalExpense));
    }
}
