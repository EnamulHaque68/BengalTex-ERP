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

/// <summary>
/// One leg of an auto-journal — debit OR credit to the account with the given code, optionally
/// tagged with accounting dimensions (Phase A3). Existing call sites pass three positional args;
/// <paramref name="Dims"/> defaults to null so they compile unchanged.
/// </summary>
public readonly record struct JournalPostingLine(
    string AccountCode, decimal Debit, decimal Credit, Dimensions? Dims = null);

/// <summary>
/// Accounting dimensions carried on a journal line (Phase A3). All optional — a flow tags only
/// the dimensions it knows (revenue legs carry buyer/style/order; expense legs carry a cost center).
/// </summary>
public readonly record struct Dimensions(
    int? CostCenterId = null,
    int? BuyerId = null,
    int? StyleId = null,
    long? SalesOrderId = null,
    long? ProductionOrderId = null);
