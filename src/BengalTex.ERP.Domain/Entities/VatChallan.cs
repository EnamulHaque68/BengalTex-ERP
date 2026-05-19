using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// VAT Challan — the legally required document that accompanies every VAT-able sale
/// in Bangladesh (NBR Mushok 6.3 form). Auto-generated 1-to-1 with a
/// <see cref="CustomerInvoice"/> at the moment it transitions Draft → Issued AND
/// has <see cref="CustomerInvoice.VatAmount"/> &gt; 0. VAT-exempt invoices (rate = 0)
/// do NOT get a challan.
///
/// Single-state, immutable from creation (same pattern as Receipt/Payment). To
/// "reissue", cancel and re-issue the parent invoice. Soft-deletes via cascade if
/// the parent invoice is cancelled.
///
/// No supplier-side equivalent — the supplier issues their own challan for purchases.
/// We just record their VatAmount on <see cref="SupplierInvoice"/> for input-VAT credit.
/// </summary>
public class VatChallan : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;

    public long CustomerInvoiceId { get; set; }
    public CustomerInvoice CustomerInvoice { get; set; } = null!;

    public DateOnly ChallanDate { get; set; }

    /// <summary>
    /// VAT amount snapshot at the moment the challan was issued. Mirrors
    /// <see cref="CustomerInvoice.VatAmount"/> at issue time — kept on the challan
    /// itself so the legal document is self-contained and immutable.
    /// </summary>
    public decimal VatAmount { get; set; }

    public decimal SubtotalAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Notes { get; set; }
}
