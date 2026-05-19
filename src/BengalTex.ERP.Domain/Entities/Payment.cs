using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Payment — money paid out to a supplier against a single <see cref="SupplierInvoice"/>.
/// Single-state, immutable from creation (symmetric with <see cref="Receipt"/>).
/// Creating a Payment atomically increments <see cref="SupplierInvoice.AmountPaid"/>
/// and recomputes the invoice status (PartiallyPaid / Paid). Deletion reverses the
/// increment and recomputes status; update is not allowed (delete + recreate).
///
/// Reuses the <see cref="PaymentMethod"/> enum from <see cref="Receipt"/> — same
/// cash/bank/cheque/mobile-banking options apply for outbound payments.
/// Transactional (long key) — unbounded volume.
/// </summary>
public class Payment : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;

    public long SupplierInvoiceId { get; set; }
    public SupplierInvoice SupplierInvoice { get; set; } = null!;

    public DateOnly PaymentDate { get; set; }

    /// <summary>Amount paid. Must be &gt; 0.</summary>
    public decimal Amount { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    /// <summary>Cheque #, transaction ID, bKash trxId, etc.</summary>
    public string? ReferenceNumber { get; set; }

    public string? Notes { get; set; }
}
