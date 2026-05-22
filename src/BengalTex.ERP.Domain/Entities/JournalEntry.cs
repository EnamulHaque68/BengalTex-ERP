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
    Posted = 2
}
