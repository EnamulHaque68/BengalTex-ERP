using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// A double-entry journal voucher. The sum of line debits must equal the sum of line credits.
/// Draft is editable; Posting freezes it and is what makes it part of the General Ledger /
/// Trial Balance. <see cref="SourceType"/>/<see cref="SourceId"/> let auto-generated entries
/// (from GRN, Invoice, Payment, Payroll …) trace back to their originating document.
/// </summary>
public class JournalEntry : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;

    public DateOnly EntryDate { get; set; }

    /// <summary>External reference (cheque no, document no, etc.).</summary>
    public string? Reference { get; set; }

    public string? Narration { get; set; }

    public JournalEntryStatus Status { get; set; } = JournalEntryStatus.Draft;

    /// <summary>
    /// Voucher classification (Phase A1). Drives the numbering series (JV/RV/PV/CV/OB/CL) and
    /// report behaviour — <see cref="VoucherType.Closing"/> entries are excluded from period
    /// P&amp;L / Trial-Balance reports (they would otherwise zero the closed year's figures).
    /// Existing rows are backfilled to <see cref="VoucherType.Journal"/>.
    /// </summary>
    public VoucherType VoucherType { get; set; } = VoucherType.Journal;

    /// <summary>The accounting period this entry was posted into (stamped at post time; null pre-fiscal-setup).</summary>
    public int? AccountingPeriodId { get; set; }
    public AccountingPeriod? AccountingPeriod { get; set; }

    /// <summary>Set on a reversal entry — the posted voucher this entry mirrors.</summary>
    public long? ReversedEntryId { get; set; }
    public JournalEntry? ReversedEntry { get; set; }

    /// <summary>Mandatory user-supplied reason when this entry is a reversal.</summary>
    public string? ReversalReason { get; set; }

    // ── Originating document for auto-generated entries (null for manual vouchers) ──
    public string? SourceType { get; set; }
    public long? SourceId { get; set; }
    public string? SourceCode { get; set; }

    public DateTimeOffset? PostedAt { get; set; }
    public string? PostedBy { get; set; }

    public ICollection<JournalEntryLine> Lines { get; set; } = new List<JournalEntryLine>();
}

/// <summary>
/// One leg of a <see cref="JournalEntry"/> — a debit OR a credit to a single (detail) account.
/// Exactly one of <see cref="Debit"/> / <see cref="Credit"/> is non-zero per line.
/// </summary>
public class JournalEntryLine : BaseTransactionalEntity
{
    public long JournalEntryId { get; set; }
    public JournalEntry JournalEntry { get; set; } = null!;

    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;

    public decimal Debit { get; set; }
    public decimal Credit { get; set; }

    public string? LineNarration { get; set; }

    public int SortOrder { get; set; }
}

public enum JournalEntryStatus
{
    Draft = 1,
    Posted = 2,
    PendingApproval = 3   // over-threshold manual voucher awaiting sign-off (Phase A1)
}

/// <summary>
/// Voucher taxonomy (Phase A1). Each type numbers from its own series:
/// Journal=JV, Receipt=RV, Payment=PV, Contra=CV, Opening=OB, Closing=CL.
/// </summary>
public enum VoucherType
{
    Journal = 1,   // manual JVs + auto-flows not otherwise classified
    Receipt = 2,   // money-in auto-journals (customer receipts)
    Payment = 3,   // money-out auto-journals (supplier payments, expenses)
    Contra = 4,    // fund transfers between cash/bank accounts
    Opening = 5,   // opening-balance import
    Closing = 6    // year-end close (excluded from period P&L / TB)
}
