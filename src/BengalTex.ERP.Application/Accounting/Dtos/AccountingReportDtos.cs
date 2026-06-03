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

// ─── Cash Book / Bank Book (Reports v2) ────────────────────────────────────
public record CashBookLineDto(
    DateOnly EntryDate,
    string JournalCode,
    string? Narration,
    decimal Receipt,            // money in  (debit on cash/bank)
    decimal Payment,            // money out (credit on cash/bank)
    decimal RunningBalance);    // always +ve on debit side for asset accounts

public record CashBookDto(
    string AccountCode,
    string AccountName,
    DateOnly FromDate,
    DateOnly ToDate,
    decimal OpeningBalance,
    decimal TotalReceipts,
    decimal TotalPayments,
    decimal ClosingBalance,
    IReadOnlyList<CashBookLineDto> Lines);

// ─── Day Book (Reports v2) ─────────────────────────────────────────────────
public record DayBookLineDto(
    int AccountId,
    string AccountCode,
    string AccountName,
    decimal Debit,
    decimal Credit,
    string? LineNarration);

public record DayBookEntryDto(
    long JournalEntryId,
    string JournalCode,
    DateOnly EntryDate,
    string? Reference,
    string? Narration,
    string? SourceType,
    string? SourceCode,
    decimal TotalDebit,
    decimal TotalCredit,
    IReadOnlyList<DayBookLineDto> Lines);

public record DayBookDto(
    DateOnly FromDate,
    DateOnly ToDate,
    decimal TotalDebit,
    decimal TotalCredit,
    IReadOnlyList<DayBookEntryDto> Entries);
