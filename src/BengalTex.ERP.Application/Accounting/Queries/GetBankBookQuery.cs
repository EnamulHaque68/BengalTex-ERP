using BengalTex.ERP.Application.Accounting.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Accounting.Queries;

/// <summary>
/// Bank Book — chronological ledger over a date range for either ONE bank account (via its
/// <see cref="BankAccount.LedgerAccountId"/>) or the AGGREGATE of all bank-type postings
/// (seeded Bank ledger account 1120). Same shape as Cash Book (Receipt/Payment columns +
/// running balance). When <paramref name="BankAccountId"/> is null, aggregates over account 1120.
/// </summary>
public sealed record GetBankBookQuery(int? BankAccountId, DateOnly FromDate, DateOnly ToDate)
    : IRequest<ApiResponse<CashBookDto>>;

internal sealed class GetBankBookQueryHandler
    : IRequestHandler<GetBankBookQuery, ApiResponse<CashBookDto>>
{
    private readonly IRepository<JournalEntryLine, long> _lineRepo;
    private readonly IRepository<Domain.Entities.Account> _accountRepo;
    private readonly IRepository<BankAccount> _bankRepo;

    public GetBankBookQueryHandler(
        IRepository<JournalEntryLine, long> lineRepo,
        IRepository<Domain.Entities.Account> accountRepo,
        IRepository<BankAccount> bankRepo)
    {
        _lineRepo = lineRepo;
        _accountRepo = accountRepo;
        _bankRepo = bankRepo;
    }

    public async Task<ApiResponse<CashBookDto>> Handle(GetBankBookQuery q, CancellationToken ct)
    {
        int accountId;
        string accountCode, accountName;

        if (q.BankAccountId.HasValue)
        {
            var bank = await _bankRepo.Query()
                .Include(b => b.LedgerAccount)
                .FirstOrDefaultAsync(b => b.Id == q.BankAccountId.Value, ct);
            if (bank is null) return ApiResponse<CashBookDto>.Fail("Bank account not found.");
            if (bank.LedgerAccountId is null || bank.LedgerAccount is null)
                return ApiResponse<CashBookDto>.Fail(
                    $"Bank '{bank.AccountName}' is not linked to a Chart-of-Accounts ledger node. " +
                    "Edit it under Master Setup → Bank Accounts and pick a Bank-type ledger account.");

            accountId = bank.LedgerAccount.Id;
            accountCode = bank.LedgerAccount.Code;
            accountName = $"{bank.AccountName} ({bank.BankName}{(string.IsNullOrEmpty(bank.AccountNumber) ? "" : " · " + bank.AccountNumber)})";
        }
        else
        {
            var ledger = await _accountRepo.Query()
                .FirstOrDefaultAsync(a => a.Code == LedgerAccounts.Bank, ct);
            if (ledger is null) return ApiResponse<CashBookDto>.Fail("Bank ledger account (1120) is not seeded.");
            accountId = ledger.Id;
            accountCode = ledger.Code;
            accountName = $"All Banks ({ledger.Name})";
        }

        var dto = await GetCashBookQueryHandler.BuildLedgerSummaryAsync(
            _lineRepo, accountId, accountCode, accountName, q.FromDate, q.ToDate, ct);
        return ApiResponse<CashBookDto>.Ok(dto);
    }
}
