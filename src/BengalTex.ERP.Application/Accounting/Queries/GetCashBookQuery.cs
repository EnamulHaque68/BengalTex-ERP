using BengalTex.ERP.Application.Accounting.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Accounting.Queries;

/// <summary>
/// Cash Book — chronological ledger of the seeded Cash account (1110) with daily running
/// balance. Debit (receipts) and Credit (payments) columns from the company's perspective.
/// Opening balance = posted activity strictly before FromDate.
/// </summary>
public sealed record GetCashBookQuery(DateOnly FromDate, DateOnly ToDate)
    : IRequest<ApiResponse<CashBookDto>>;

internal sealed class GetCashBookQueryHandler
    : IRequestHandler<GetCashBookQuery, ApiResponse<CashBookDto>>
{
    private readonly IRepository<JournalEntryLine, long> _lineRepo;
    private readonly IRepository<Domain.Entities.Account> _accountRepo;

    public GetCashBookQueryHandler(
        IRepository<JournalEntryLine, long> lineRepo,
        IRepository<Domain.Entities.Account> accountRepo)
    {
        _lineRepo = lineRepo;
        _accountRepo = accountRepo;
    }

    public async Task<ApiResponse<CashBookDto>> Handle(GetCashBookQuery q, CancellationToken ct)
    {
        var cash = await _accountRepo.Query()
            .FirstOrDefaultAsync(a => a.Code == LedgerAccounts.Cash, ct);
        if (cash is null)
            return ApiResponse<CashBookDto>.Fail("Cash account (1110) is not seeded.");

        var dto = await BuildLedgerSummaryAsync(_lineRepo, cash.Id, cash.Code, cash.Name, q.FromDate, q.ToDate, ct);
        return ApiResponse<CashBookDto>.Ok(dto);
    }

    /// <summary>
    /// Build a cash-book-shaped summary for any account. Reused by Bank Book.
    /// Caller supplies (accountId, code, name); we sum posted debits/credits before FromDate
    /// for opening balance, then walk in-range lines chronologically.
    /// </summary>
    internal static async Task<CashBookDto> BuildLedgerSummaryAsync(
        IRepository<JournalEntryLine, long> lineRepo,
        int accountId,
        string accountCode,
        string accountName,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken ct)
    {
        // Opening: posted activity strictly before FromDate
        var before = await lineRepo.Query()
            .Where(l => l.AccountId == accountId
                     && l.JournalEntry.Status == JournalEntryStatus.Posted
                     && l.JournalEntry.EntryDate < fromDate)
            .GroupBy(l => 1)
            .Select(g => new { Debit = g.Sum(x => x.Debit), Credit = g.Sum(x => x.Credit) })
            .FirstOrDefaultAsync(ct);
        var opening = before is null ? 0m : before.Debit - before.Credit;

        var inRange = await lineRepo.Query()
            .Where(l => l.AccountId == accountId
                     && l.JournalEntry.Status == JournalEntryStatus.Posted
                     && l.JournalEntry.EntryDate >= fromDate
                     && l.JournalEntry.EntryDate <= toDate)
            .OrderBy(l => l.JournalEntry.EntryDate).ThenBy(l => l.JournalEntryId).ThenBy(l => l.SortOrder)
            .Select(l => new
            {
                l.JournalEntry.EntryDate,
                l.JournalEntry.Code,
                Narration = l.LineNarration ?? l.JournalEntry.Narration,
                l.Debit,
                l.Credit
            })
            .ToListAsync(ct);

        var lines = new List<CashBookLineDto>(inRange.Count);
        var running = opening;
        decimal totalReceipts = 0m, totalPayments = 0m;
        foreach (var l in inRange)
        {
            running += l.Debit - l.Credit;
            totalReceipts += l.Debit;
            totalPayments += l.Credit;
            lines.Add(new CashBookLineDto(l.EntryDate, l.Code, l.Narration, l.Debit, l.Credit, running));
        }

        return new CashBookDto(
            accountCode, accountName,
            fromDate, toDate,
            opening, totalReceipts, totalPayments, running, lines);
    }
}
