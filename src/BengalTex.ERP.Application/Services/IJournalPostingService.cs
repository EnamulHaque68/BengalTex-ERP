namespace BengalTex.ERP.Application.Services;

/// <summary>
/// Creates auto-generated, already-POSTED journal entries from source documents (Customer/
/// Supplier Invoice, Receipt, Payment, …). Accounts are resolved by their seeded Chart-of-
/// Accounts <c>Code</c> (see <c>LedgerAccounts</c>). Does NOT call SaveChanges — the calling
/// command commits, so the journal posts atomically with the document's own state change.
///
/// The supplied lines must balance (Σ debit == Σ credit); zero-amount lines are dropped. The
/// resulting entry is tagged with SourceType/SourceId/SourceCode for full traceability and so a
/// later cancel/delete can post a mirror reversal.
/// </summary>
public interface IJournalPostingService
{
    Task PostAsync(
        DateOnly date,
        string narration,
        string sourceType,
        long sourceId,
        string sourceCode,
        IReadOnlyList<JournalPostingLine> lines,
        CancellationToken ct = default);
}

/// <summary>One leg of an auto-journal — debit OR credit to the account with the given code.</summary>
public readonly record struct JournalPostingLine(string AccountCode, decimal Debit, decimal Credit);
