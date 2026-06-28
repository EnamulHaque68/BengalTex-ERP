using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Receipt — money received from a customer against a single <see cref="CustomerInvoice"/>.
/// Lifecycle: Draft → Posted → (Cancelled). Creating a Receipt records a <b>Draft</b> only and
/// does NOT touch the invoice. <b>Posting</b> the receipt atomically increments
/// <see cref="CustomerInvoice.AmountPaid"/>, recomputes the invoice status (PartiallyPaid if
/// &lt; Total, Paid if &gt;= Total) and posts the cash/AR journal. <b>Cancelling</b> a posted
/// receipt reverses that effect; cancelling a draft simply voids it.
///
/// Multiple Receipts may exist for a single invoice (partial payments over time).
/// Transactional (long key) — unbounded volume.
/// </summary>
public class Receipt : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;

    public long CustomerInvoiceId { get; set; }
    public CustomerInvoice CustomerInvoice { get; set; } = null!;

    public DateOnly ReceiptDate { get; set; }

    /// <summary>Amount received, in the invoice's currency. Must be &gt; 0.</summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// BDT per 1 unit of the invoice's currency at RECEIPT time (the rate the money actually
    /// came in at). Default 1 (BDT invoices). When it differs from the invoice's locked rate,
    /// the receipt journal recognizes a realized FX gain/loss for the difference.
    /// </summary>
    public decimal ExchangeRate { get; set; } = 1m;

    public PaymentMethod PaymentMethod { get; set; }

    /// <summary>
    /// Draft → Posted → Cancelled. Only a <see cref="ReceiptStatus.Posted"/> receipt has affected
    /// the parent invoice's <c>AmountPaid</c>/status. New receipts start as Draft.
    /// </summary>
    public ReceiptStatus Status { get; set; } = ReceiptStatus.Draft;

    public DateTimeOffset? PostedAt { get; set; }
    public string? PostedBy { get; set; }

    /// <summary>Cheque #, transaction ID, bKash trxId, etc.</summary>
    public string? ReferenceNumber { get; set; }

    public string? Notes { get; set; }
}

public enum PaymentMethod
{
    Cash = 1,
    BankTransfer = 2,
    Cheque = 3,
    MobileBanking = 4,    // bKash, Nagad, Rocket
    Other = 99
}

public enum ReceiptStatus
{
    Draft = 1,        // recorded, not yet applied to the invoice
    Posted = 2,       // applied — reduced the invoice outstanding
    Cancelled = 3     // voided (a posted receipt's effect was reversed)
}
