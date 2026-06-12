using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Debit Note — a non-stock adjustment WE issue to a SUPPLIER that reduces the
/// outstanding balance we owe on one of their <see cref="SupplierInvoice"/>s
/// (e.g. price correction, post-purchase discount, quality allowance without
/// physical return). On Issue the adjustment amount is added to
/// <see cref="SupplierInvoice.AmountPaid"/>, the invoice status is recomputed,
/// and an auto-journal posts Dr Accounts Payable / Cr Purchase Returns &amp; Allowances.
/// Cancel posts a mirror reversal and restores the invoice balance.
///
/// For PHYSICAL returns to supplier (RM going back, stock moving), use
/// <see cref="SupplierReturnNote"/> instead.
/// </summary>
public class DebitNote : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;   // "DBN-####"

    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    public long SupplierInvoiceId { get; set; }
    public SupplierInvoice SupplierInvoice { get; set; } = null!;

    public DateOnly IssueDate { get; set; }

    public CreditDebitNoteReason Reason { get; set; } = CreditDebitNoteReason.PriceCorrection;

    /// <summary>Adjustment amount in the source invoice's currency. Must be &gt; 0.</summary>
    public decimal Amount { get; set; }

    /// <summary>Snapshot of source invoice's currency &amp; rate (auto-set on Create).</summary>
    public int CurrencyId { get; set; }
    public Currency Currency { get; set; } = null!;
    public decimal ExchangeRate { get; set; } = 1m;

    public DebitNoteStatus Status { get; set; } = DebitNoteStatus.Draft;

    /// <summary>
    /// Optional link to the physical return (SRN) this debit note recovers — set when the
    /// DBN is raised from a posted SupplierReturnNote. Traceability only; the SRN moved the
    /// stock, this DBN settles the money.
    /// </summary>
    public long? SupplierReturnNoteId { get; set; }
    public SupplierReturnNote? SupplierReturnNote { get; set; }

    public DateTimeOffset? IssuedAt { get; set; }
    public string? IssuedBy { get; set; }

    public string? Notes { get; set; }
}

public enum DebitNoteStatus
{
    Draft = 1,
    Issued = 2,
    Cancelled = 3
}
