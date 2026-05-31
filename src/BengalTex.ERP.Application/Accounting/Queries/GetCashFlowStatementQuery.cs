using BengalTex.ERP.Application.Accounting.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Accounting.Queries;

/// <summary>
/// Cash Flow Statement for a period — categorises every posted journal line that hits
/// Cash (1110) or Bank (1120) into Operating / Investing / Financing per accounting
/// standards. Categorisation key = JournalEntry.SourceType:
///   Operating  : Receipt, Payment, Payslip, Expense, FestivalBonus, CustomerInvoice,
///                SupplierInvoice (anything not otherwise classified)
///   Investing  : (reserved — asset purchases will tag SourceType="FixedAsset" in v1c)
///   Financing  : (reserved — loan/capital transactions will tag accordingly)
///
/// Opening balance = sum of Dr−Cr on Cash+Bank accounts for ALL posted entries before
/// FromDate. Closing balance = Opening + Σ section net.
/// </summary>
public sealed record GetCashFlowStatementQuery(DateOnly FromDate, DateOnly ToDate)
    : IRequest<ApiResponse<CashFlowStatementDto>>;

internal sealed class GetCashFlowStatementQueryHandler
    : IRequestHandler<GetCashFlowStatementQuery, ApiResponse<CashFlowStatementDto>>
{
    private readonly IRepository<JournalEntryLine, long> _lineRepo;
    private readonly IRepository<Domain.Entities.Account> _accountRepo;

    public GetCashFlowStatementQueryHandler(
        IRepository<JournalEntryLine, long> lineRepo,
        IRepository<Domain.Entities.Account> accountRepo)
    {
        _lineRepo = lineRepo;
        _accountRepo = accountRepo;
    }

    public async Task<ApiResponse<CashFlowStatementDto>> Handle(
        GetCashFlowStatementQuery request, CancellationToken ct)
    {
        // Resolve the cash + bank account Ids by their seeded codes
        var cashAccounts = await _accountRepo.Query()
            .Where(a => a.Code == LedgerAccounts.Cash || a.Code == LedgerAccounts.Bank)
            .Select(a => new { a.Id, a.Code, a.Name })
            .ToListAsync(ct);
        if (cashAccounts.Count == 0)
            return ApiResponse<CashFlowStatementDto>.Fail("Cash/Bank accounts not found in Chart of Accounts.");
        var cashAccountIds = cashAccounts.Select(a => a.Id).ToHashSet();
        var accNameById = cashAccounts.ToDictionary(a => a.Id, a => a.Name);

        // ── Opening balance: Σ (Dr − Cr) on Cash+Bank for posted entries before FromDate ──
        var openingTotals = await _lineRepo.Query()
            .Where(l => l.JournalEntry.Status == JournalEntryStatus.Posted
                     && l.JournalEntry.EntryDate < request.FromDate
                     && cashAccountIds.Contains(l.AccountId))
            .GroupBy(l => 1)
            .Select(g => new { Debit = g.Sum(x => x.Debit), Credit = g.Sum(x => x.Credit) })
            .FirstOrDefaultAsync(ct);
        var openingBalance = (openingTotals?.Debit ?? 0m) - (openingTotals?.Credit ?? 0m);

        // ── Period lines: every posted journal line that hits Cash/Bank in range ──
        var periodLines = await _lineRepo.Query()
            .Where(l => l.JournalEntry.Status == JournalEntryStatus.Posted
                     && l.JournalEntry.EntryDate >= request.FromDate
                     && l.JournalEntry.EntryDate <= request.ToDate
                     && cashAccountIds.Contains(l.AccountId))
            .OrderBy(l => l.JournalEntry.EntryDate).ThenBy(l => l.Id)
            .Select(l => new
            {
                Date = l.JournalEntry.EntryDate,
                SourceType = l.JournalEntry.SourceType,
                SourceCode = l.JournalEntry.SourceCode,
                Narration = l.JournalEntry.Narration,
                AccountId = l.AccountId,
                l.Debit,
                l.Credit
            })
            .ToListAsync(ct);

        // Classify + transform
        var lines = periodLines.Select(l => new CashFlowLineDto(
            l.Date,
            string.IsNullOrEmpty(l.SourceType) ? "Manual" : l.SourceType,
            l.SourceCode,
            accNameById.TryGetValue(l.AccountId, out var n) ? n : "Cash/Bank",
            l.Narration ?? string.Empty,
            l.Debit,         // Inflow  = Dr Cash/Bank
            l.Credit         // Outflow = Cr Cash/Bank
        )).ToList();

        var sections = new List<CashFlowSectionDto>
        {
            Section("Operating", lines.Where(l => Categorise(l.SourceType) == "Operating").ToList()),
            Section("Investing", lines.Where(l => Categorise(l.SourceType) == "Investing").ToList()),
            Section("Financing", lines.Where(l => Categorise(l.SourceType) == "Financing").ToList())
        };

        var netChange = sections.Sum(s => s.NetChange);
        var closing = openingBalance + netChange;

        return ApiResponse<CashFlowStatementDto>.Ok(new CashFlowStatementDto(
            request.FromDate, request.ToDate, openingBalance, sections, netChange, closing));
    }

    private static string Categorise(string sourceType) => sourceType switch
    {
        // v1c will add Investing (FixedAsset purchases) + Financing (Loan/Capital)
        _ => "Operating"
    };

    private static CashFlowSectionDto Section(string name, IReadOnlyList<CashFlowLineDto> lines)
    {
        var inflow = lines.Sum(l => l.Inflow);
        var outflow = lines.Sum(l => l.Outflow);
        return new CashFlowSectionDto(name, lines, inflow, outflow, inflow - outflow);
    }
}
