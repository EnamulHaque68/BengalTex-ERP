using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Credit Note — a non-stock adjustment WE issue to a CUSTOMER that reduces the
/// outstanding balance on one of their <see cref="CustomerInvoice"/>s (e.g. price
/// correction, post-sale discount, write-off, quality allowance without physical
/// return). On Issue the adjustment amount is added to <see cref="CustomerInvoice.AmountPaid"/>
/// (a "non-cash settlement"), the invoice status is recomputed, and an auto-journal
/// posts Dr Sales Returns &amp; Allowances / Cr Accounts Receivable. Cancel posts a
/// mirror reversal and restores the invoice balance.
///
/// For PHYSICAL returns (goods coming back, stock moving), use
/// <see cref="CustomerReturnNote"/> instead.
/// </summary>
public class CreditNote : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;   // "CN-####"

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public long CustomerInvoiceId { get; set; }
    public CustomerInvoice CustomerInvoice { get; set; } = null!;

    public DateOnly IssueDate { get; set; }

    public CreditDebitNoteReason Reason { get; set; } = CreditDebitNoteReason.PriceCorrection;

    /// <summary>Adjustment amount in the source invoice's currency. Must be &gt; 0.</summary>
    public decimal Amount { get; set; }

    /// <summary>Snapshot of source invoice's currency &amp; rate (auto-set on Create).</summary>
    public int CurrencyId { get; set; }
    public Currency Currency { get; set; } = null!;
    public decimal ExchangeRate { get; set; } = 1m;

    public CreditNoteStatus Status { get; set; } = CreditNoteStatus.Draft;

    public DateTimeOffset? IssuedAt { get; set; }
    public string? IssuedBy { get; set; }

    public string? Notes { get; set; }
}

public enum CreditNoteStatus
{
    Draft = 1,
    Issued = 2,
    Cancelled = 3
}

/// <summary>Shared reason enum for both Credit (customer) and Debit (supplier) notes.</summary>
public enum CreditDebitNoteReason
{
    PriceCorrection = 1,
    QualityAllowance = 2,         // partial allowance without physical return
    Discount = 3,                 // post-sale discount granted
    WriteOff = 4,
    Other = 99
}
