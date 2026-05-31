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

// ─── Cash Flow Statement (period) ──────────────────────────────────────────

/// <summary>One cash movement: a posted journal line touching Cash (1110) or Bank (1120).</summary>
public record CashFlowLineDto(
    DateOnly Date,
    string SourceType,         // "Receipt", "Payment", "Payslip", "Expense", "FestivalBonus", "Manual", ...
    string? SourceCode,        // e.g. "RCT-0001"; null for manual JV
    string AccountName,        // "Cash in Hand" | "Bank Accounts"
    string Narration,
    decimal Inflow,            // positive when cash came IN (Dr Cash/Bank)
    decimal Outflow);          // positive when cash went OUT (Cr Cash/Bank)

public record CashFlowSectionDto(
    string SectionName,                              // "Operating" | "Investing" | "Financing"
    IReadOnlyList<CashFlowLineDto> Lines,
    decimal TotalInflow,
    decimal TotalOutflow,
    decimal NetChange);                              // Inflow − Outflow

public record CashFlowStatementDto(
    DateOnly FromDate,
    DateOnly ToDate,
    decimal OpeningCashBalance,                      // Cash + Bank net Dr balance up to (FromDate − 1)
    IReadOnlyList<CashFlowSectionDto> Sections,      // Operating / Investing / Financing in this order
    decimal NetCashChange,                           // Σ section NetChange
    decimal ClosingCashBalance);                     // Opening + NetCashChange
