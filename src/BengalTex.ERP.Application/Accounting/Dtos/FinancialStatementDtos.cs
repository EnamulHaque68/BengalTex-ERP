namespace BengalTex.ERP.Application.Accounting.Dtos;

// ─── Profit & Loss (period) ───────────────────────────────────────────────────
public record StatementLineDto(
    int AccountId,
    string AccountCode,
    string AccountName,
    decimal Amount);

public record ProfitAndLossDto(
    DateOnly FromDate,
    DateOnly ToDate,
    IReadOnlyList<StatementLineDto> Income,
    decimal TotalIncome,
    IReadOnlyList<StatementLineDto> Expenses,
    decimal TotalExpense,
    decimal NetProfit);

// ─── Balance Sheet (as of) ──────────────────────────────────────────────────
public record BalanceSheetDto(
    DateOnly AsOfDate,
    IReadOnlyList<StatementLineDto> Assets,
    decimal TotalAssets,
    IReadOnlyList<StatementLineDto> Liabilities,
    decimal TotalLiabilities,
    IReadOnlyList<StatementLineDto> Equity,
    decimal CurrentEarnings,        // computed Income − Expense up to the date, shown under Equity
    decimal TotalEquity,            // equity accounts + CurrentEarnings
    decimal TotalLiabilitiesAndEquity,
    bool IsBalanced);
