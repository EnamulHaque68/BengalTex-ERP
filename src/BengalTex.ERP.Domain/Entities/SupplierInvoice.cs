using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Supplier Invoice — bill we receive from a <see cref="Supplier"/> for a fulfilled
/// (Received/Closed) <see cref="PurchaseOrder"/>. Lifecycle:
/// Draft → Approved → (PartiallyPaid → Paid); cancellable from Draft or Approved
/// as long as no <see cref="Payment"/>s have been made. Approval is the internal
/// authorization to pay (the equivalent of "Issued" on customer-side invoices —
/// we don't issue these, we record + approve the supplier's bill).
///
/// <see cref="TotalAmount"/> is a snapshot (sum of line totals, computed on Save).
/// <see cref="AmountPaid"/> is denormalized and maintained by Payment create/delete.
/// Transactional (long key) — unbounded volume.
///
/// Symmetric counterpart of <see cref="CustomerInvoice"/> on the payables side.
/// </summary>
public class SupplierInvoice : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;

    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    public long PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    /// <summary>Supplier's own invoice/document number (the number THEY printed on the bill).</summary>
    public string? SupplierInvoiceNumber { get; set; }

    public DateOnly InvoiceDate { get; set; }

    /// <summary>Default = InvoiceDate + Supplier.PaymentTermsDays; editable.</summary>
    public DateOnly DueDate { get; set; }

    public SupplierInvoiceStatus Status { get; set; } = SupplierInvoiceStatus.Draft;

    /// <summary>Sum of line totals — written on Save, locked at Approve.</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Running sum of all non-deleted <see cref="Payment"/>s. Updated atomically.</summary>
    public decimal AmountPaid { get; set; }

    public DateTimeOffset? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }

    public string? Notes { get; set; }

    public ICollection<SupplierInvoiceLine> Lines { get; set; } = new List<SupplierInvoiceLine>();
}

/// <summary>
/// A single raw-material line on a <see cref="SupplierInvoice"/>. Editable while the
/// parent is Draft; locked once Approved.
/// </summary>
public class SupplierInvoiceLine : BaseTransactionalEntity
{
    public long SupplierInvoiceId { get; set; }
    public SupplierInvoice SupplierInvoice { get; set; } = null!;

    public int RawMaterialId { get; set; }
    public RawMaterial RawMaterial { get; set; } = null!;

    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public int SortOrder { get; set; }

    public string? LineNotes { get; set; }
}

public enum SupplierInvoiceStatus
{
    Draft = 1,
    Approved = 2,         // we've authorized this for payment
    PartiallyPaid = 3,    // 0 < AmountPaid < TotalAmount
    Paid = 4,             // AmountPaid >= TotalAmount
    Cancelled = 5         // only allowed when AmountPaid = 0
}
