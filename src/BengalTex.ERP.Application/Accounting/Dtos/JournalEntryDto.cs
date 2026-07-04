namespace BengalTex.ERP.Application.Accounting.Dtos;

public record JournalEntryDto(
    long Id,
    string Code,
    DateOnly EntryDate,
    string? Reference,
    string? Narration,
    string Status,
    string? SourceType,
    long? SourceId,
    string? SourceCode,
    DateTimeOffset? PostedAt,
    string? PostedBy,
    decimal TotalDebit,
    decimal TotalCredit,
    IReadOnlyList<JournalEntryLineDto> Lines,
    // ── Phase A1 — voucher taxonomy + reversal linkage ──
    string VoucherType = "Journal",
    long? ReversedEntryId = null,
    string? ReversedEntryCode = null,
    string? ReversalReason = null);

public record JournalEntryLineDto(
    long Id,
    int AccountId,
    string AccountCode,
    string AccountName,
    decimal Debit,
    decimal Credit,
    string? LineNarration,
    int SortOrder);

public record JournalEntryListItemDto(
    long Id,
    string Code,
    DateOnly EntryDate,
    string? Reference,
    string? Narration,
    string Status,
    decimal Amount,           // balanced total (= total debit = total credit)
    int LineCount,
    string? SourceType,
    string? SourceCode,
    string VoucherType = "Journal");   // Phase A1
