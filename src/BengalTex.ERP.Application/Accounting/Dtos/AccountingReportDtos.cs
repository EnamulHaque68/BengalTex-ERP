namespace BengalTex.ERP.Application.Accounting.Dtos;

// ─── Trial Balance ───────────────────────────────────────────────────────────
public record TrialBalanceRowDto(
    int AccountId,
    string AccountCode,
    string AccountName,
    string AccountType,
    decimal DebitBalance,
    decimal CreditBalance);

public record TrialBalanceDto(
    DateOnly AsOfDate,
    IReadOnlyList<TrialBalanceRowDto> Rows,
    decimal TotalDebit,
    decimal TotalCredit,
    bool IsBalanced);

// ─── General Ledger (one account) ──────────────────────────────────────────────
public record GeneralLedgerLineDto(
    DateOnly EntryDate,
    string JournalCode,
    string? Narration,
    decimal Debit,
    decimal Credit,
    decimal RunningBalance);     // signed on the account's normal side

public record GeneralLedgerDto(
    int AccountId,
    string AccountCode,
    string AccountName,
    string NormalBalance,
    DateOnly FromDate,
    DateOnly ToDate,
    decimal OpeningBalance,
    decimal TotalDebit,
    decimal TotalCredit,
    decimal ClosingBalance,
    IReadOnlyList<GeneralLedgerLineDto> Lines);
