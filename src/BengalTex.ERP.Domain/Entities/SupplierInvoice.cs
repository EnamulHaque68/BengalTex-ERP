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

    /// <summary>Transaction currency (Phase 21). All amounts on this invoice are in this currency.</summary>
    public int CurrencyId { get; set; }
    public Currency Currency { get; set; } = null!;

    /// <summary>BDT per 1 unit of <see cref="Currency"/>, snapshot at transaction time. 1 for BDT.</summary>
    public decimal ExchangeRate { get; set; } = 1m;

    /// <summary>
    /// VAT rate as a decimal fraction (0.15 = 15%). Default 0.15 for Bangladesh standard
    /// rate; set to 0 for VAT-exempt invoices. Existing pre-Phase-12 rows backfilled to 0.
    /// </summary>
    public decimal VatRate { get; set; }

    /// <summary>
    /// Sum of (line.Quantity × line.UnitPrice) — net of VAT. Computed on Save. Pre-VAT
    /// equivalent of the old TotalAmount field.
    /// </summary>
    public decimal SubtotalAmount { get; set; }

    /// <summary>VAT charged on this invoice. Computed as SubtotalAmount × VatRate on Save.</summary>
    public decimal VatAmount { get; set; }

    /// <summary>
    /// Gross — what the supplier is owed (SubtotalAmount + VatAmount). Locked at Approve.
    /// AmountPaid is measured against this; receipt/payment overpayment checks compare to this.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Running sum of all non-deleted <see cref="Payment"/>s. Updated atomically.</summary>
    public decimal AmountPaid { get; set; }

    public DateTimeOffset? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }

    /// <summary>
    /// Go-live opening bill (Phase A1). When true, approving posts NO journal — the GL balance
    /// comes from the opening-balance voucher — while payments and ageing work normally.
    /// </summary>
    public bool IsOpening { get; set; }

    public string? Notes { get; set; }

    public ICollection<SupplierInvoiceLine> Lines { get; set; } = new List<SupplierInvoiceLine>();
}

/// <summary>
/// A single line on a <see cref="SupplierInvoice"/>. Editable while the parent is Draft;
/// locked once Approved.
///
/// A line is EITHER a material line (<see cref="RawMaterialId"/> set → clears GR/IR against the
/// receipt) OR a service line (<see cref="AccountId"/> set → debits the picked expense account,
/// e.g. C&amp;F, freight, repairs — never touching stock or inventory). Exactly one of the two is
/// non-null, enforced by a DB check constraint. Existing pre-A2 rows are all material lines.
/// </summary>
public class SupplierInvoiceLine : BaseTransactionalEntity
{
    public long SupplierInvoiceId { get; set; }
    public SupplierInvoice SupplierInvoice { get; set; } = null!;

    /// <summary>Material line — the raw material billed (null on a service line).</summary>
    public int? RawMaterialId { get; set; }
    public RawMaterial? RawMaterial { get; set; }

    /// <summary>Service line — the expense account to debit (null on a material line). Phase A2.</summary>
    public int? AccountId { get; set; }
    public Account? Account { get; set; }

    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public int SortOrder { get; set; }

    public string? LineNotes { get; set; }

    /// <summary>Convenience — true when this is a service (expense-account) line.</summary>
    public bool IsService => AccountId.HasValue;
}

public enum SupplierInvoiceStatus
{
    Draft = 1,
    Approved = 2,         // we've authorized this for payment
    PartiallyPaid = 3,    // 0 < AmountPaid < TotalAmount
    Paid = 4,             // AmountPaid >= TotalAmount
    Cancelled = 5         // only allowed when AmountPaid = 0
}
