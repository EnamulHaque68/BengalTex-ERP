using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Receipt — money received from a customer against a single <see cref="CustomerInvoice"/>.
/// Single-state, immutable from creation. Creating a Receipt atomically increments
/// <see cref="CustomerInvoice.AmountPaid"/> and recomputes the invoice status
/// (PartiallyPaid if &lt; Total, Paid if &gt;= Total). Deletion reverses the increment
/// and recomputes status; update is not allowed (delete + recreate).
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
