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

    /// <summary>Amount received. Must be &gt; 0.</summary>
    public decimal Amount { get; set; }

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
