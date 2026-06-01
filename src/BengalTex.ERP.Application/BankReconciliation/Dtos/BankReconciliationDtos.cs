namespace BengalTex.ERP.Application.BankReconciliation.Dtos;

public record BankStatementListItemDto(
    long Id,
    string Code,
    int BankAccountId,
    string BankAccountName,
    DateOnly StatementDate,
    DateOnly PeriodFromDate,
    DateOnly PeriodToDate,
    decimal OpeningBalance,
    decimal ClosingBalance,
    bool IsReconciled,
    DateTimeOffset? ReconciledAt,
    int LineCount,
    int MatchedCount,
    int UnmatchedCount);

public record BankStatementLineDto(
    long Id,
    long BankStatementId,
    DateOnly TransactionDate,
    string Description,
    string? ReferenceNumber,
    decimal Amount,                 // signed
    string Status,                  // Unmatched | Matched | Excluded
    long? MatchedJournalLineId,
    string? MatchedJournalEntryCode,
    string? MatchedJournalNarration,
    DateTimeOffset? MatchedAt,
    string? MatchedBy,
    string? Notes);

public record BankStatementDto(
    long Id,
    string Code,
    int BankAccountId,
    string BankAccountName,
    int? LedgerAccountId,
    string? LedgerAccountCode,
    string? LedgerAccountName,
    DateOnly StatementDate,
    DateOnly PeriodFromDate,
    DateOnly PeriodToDate,
    decimal OpeningBalance,
    decimal ClosingBalance,
    decimal MatchedAmount,           // Σ signed amount of matched lines
    decimal ComputedClosing,         // Opening + MatchedAmount
    bool BalancesMatch,              // ComputedClosing == ClosingBalance
    bool IsReconciled,
    DateTimeOffset? ReconciledAt,
    string? ReconciledBy,
    string? Notes,
    IReadOnlyList<BankStatementLineDto> Lines);

/// <summary>One posted journal line on a bank's ledger account, candidate for matching.</summary>
public record UnmatchedJournalLineDto(
    long Id,
    long JournalEntryId,
    string JournalEntryCode,
    DateOnly EntryDate,
    string Narration,
    string? SourceType,
    string? SourceCode,
    decimal Amount,                  // signed: + Debit (deposit to bank in ledger) − Credit (withdrawal from bank)
    decimal Debit,
    decimal Credit);
