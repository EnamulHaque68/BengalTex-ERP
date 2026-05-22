using BengalTex.ERP.Application.Accounting.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Accounting.Queries;

/// <summary>
/// General ledger for one account over a date range: opening balance (posted activity before
/// FromDate), each posted line in range with a running balance, and the closing balance — all
/// signed on the account's normal side (a positive number means the account is "up" naturally).
/// </summary>
public sealed record GetGeneralLedgerQuery(int AccountId, DateOnly FromDate, DateOnly ToDate)
    : IRequest<ApiResponse<GeneralLedgerDto>>;

internal sealed class GetGeneralLedgerQueryHandler
    : IRequestHandler<GetGeneralLedgerQuery, ApiResponse<GeneralLedgerDto>>
{
    private readonly IRepository<JournalEntryLine, long> _lineRepo;
    private readonly IRepository<Domain.Entities.Account> _accountRepo;

    public GetGeneralLedgerQueryHandler(
        IRepository<JournalEntryLine, long> lineRepo,
        IRepository<Domain.Entities.Account> accountRepo)
    {
        _lineRepo = lineRepo;
        _accountRepo = accountRepo;
    }

    public async Task<ApiResponse<GeneralLedgerDto>> Handle(
        GetGeneralLedgerQuery request, CancellationToken cancellationToken)
    {
        var account = await _accountRepo.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null) return ApiResponse<GeneralLedgerDto>.Fail("Account not found.");

        var debitNormal = AccountingMapping.IsDebitNormal(account.AccountType);
        decimal Signed(decimal debit, decimal credit) => debitNormal ? debit - credit : credit - debit;

        // Opening balance — posted activity strictly before FromDate.
        var before = await _lineRepo.Query()
            .Where(l => l.AccountId == request.AccountId
                     && l.JournalEntry.Status == JournalEntryStatus.Posted
                     && l.JournalEntry.EntryDate < request.FromDate)
            .GroupBy(l => 1)
            .Select(g => new { Debit = g.Sum(x => x.Debit), Credit = g.Sum(x => x.Credit) })
            .FirstOrDefaultAsync(cancellationToken);
        var opening = before is null ? 0m : Signed(before.Debit, before.Credit);

        // In-range posted lines, oldest first.
        var inRange = await _lineRepo.Query()
            .Where(l => l.AccountId == request.AccountId
                     && l.JournalEntry.Status == JournalEntryStatus.Posted
                     && l.JournalEntry.EntryDate >= request.FromDate
                     && l.JournalEntry.EntryDate <= request.ToDate)
            .OrderBy(l => l.JournalEntry.EntryDate).ThenBy(l => l.JournalEntryId).ThenBy(l => l.SortOrder)
            .Select(l => new
            {
                l.JournalEntry.EntryDate,
                l.JournalEntry.Code,
                Narration = l.LineNarration ?? l.JournalEntry.Narration,
                l.Debit,
                l.Credit
            })
            .ToListAsync(cancellationToken);

        var lines = new List<GeneralLedgerLineDto>();
        var running = opening;
        decimal totalDebit = 0m, totalCredit = 0m;
        foreach (var l in inRange)
        {
            running += Signed(l.Debit, l.Credit);
            totalDebit += l.Debit;
            totalCredit += l.Credit;
            lines.Add(new GeneralLedgerLineDto(l.EntryDate, l.Code, l.Narration, l.Debit, l.Credit, running));
        }

        var dto = new GeneralLedgerDto(
            account.Id, account.Code, account.Name,
            AccountingMapping.NormalBalanceOf(account.AccountType),
            request.FromDate, request.ToDate,
            opening, totalDebit, totalCredit, running, lines);

        return ApiResponse<GeneralLedgerDto>.Ok(dto);
    }
}
