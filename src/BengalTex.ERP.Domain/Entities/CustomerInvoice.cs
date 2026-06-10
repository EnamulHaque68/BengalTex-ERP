using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Customer Invoice — bill we raise to a <see cref="Customer"/> against a fulfilled
/// (Confirmed/Dispatched/Closed) <see cref="SalesOrder"/>. Lifecycle:
/// Draft → Issued → (PartiallyPaid → Paid); cancellable from Draft or Issued as long
/// as no <see cref="Receipt"/>s have been applied. Once Issued, lines are immutable;
/// only payment state changes via Receipts.
///
/// <see cref="TotalAmount"/> is a snapshot (sum of line totals, computed on Save).
/// <see cref="AmountPaid"/> is a denormalized running sum maintained by Receipt
/// create/delete — keeps status recompute O(1).
/// Transactional (long key) — unbounded volume.
/// </summary>
public class CustomerInvoice : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public long SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = null!;

    public DateOnly InvoiceDate { get; set; }

    /// <summary>Default = InvoiceDate + Customer.PaymentTermsDays; editable.</summary>
    public DateOnly DueDate { get; set; }

    public CustomerInvoiceStatus Status { get; set; } = CustomerInvoiceStatus.Draft;

    /// <summary>Transaction currency (Phase 21). All amounts on this invoice are in this currency.</summary>
    public int CurrencyId { get; set; }
    public Currency Currency { get; set; } = null!;

    /// <summary>BDT per 1 unit of <see cref="Currency"/>, snapshot at transaction time. 1 for BDT.</summary>
    public decimal ExchangeRate { get; set; } = 1m;

    /// <summary>
    /// VAT rate as a decimal fraction (0.15 = 15%). Default 0.15 for Bangladesh standard
    /// rate; set to 0 for VAT-exempt customers. Existing pre-Phase-12 rows backfilled to 0.
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
    /// Gross — what the customer owes (SubtotalAmount + VatAmount). Locked at Issue.
    /// AmountPaid is measured against this.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Running sum of all non-deleted <see cref="Receipt"/>s applied. Updated atomically.</summary>
    public decimal AmountPaid { get; set; }

    public DateTimeOffset? IssuedAt { get; set; }
    public string? IssuedBy { get; set; }

    public string? Notes { get; set; }

    // ── BD export reporting fields (EPB / Form-N / Form-EXP) ────────────────
    /// <summary>Form-EXP number issued by the exporter's bank (foreign-exchange declaration).</summary>
    public string? EpbFormNumber { get; set; }
    /// <summary>Letter-of-Credit reference (free text; for non-LC exports, leave null).</summary>
    public string? LcNumber { get; set; }
    /// <summary>Physical shipment date — when goods cleared the port. Separate from invoice date.</summary>
    public DateOnly? ShipmentDate { get; set; }

    // ── Export shipping document fields (Commercial Invoice / Packing List) ──
    /// <summary>Incoterm — FOB / CFR / CIF / EXW / DAP / etc. Free text (e.g. "FOB Chattogram").</summary>
    public string? IncoTerm { get; set; }
    /// <summary>Port of loading — typically "Chattogram, Bangladesh" or "Dhaka (Air)".</summary>
    public string? PortOfLoading { get; set; }
    /// <summary>Port of discharge — buyer-side destination port.</summary>
    public string? PortOfDischarge { get; set; }
    /// <summary>Vessel name / flight number (free text).</summary>
    public string? VesselName { get; set; }
    /// <summary>Country of final destination — override of Customer.Country if different.</summary>
    public string? CountryOfDestination { get; set; }
    /// <summary>Shipping marks &amp; numbers block (free text — appears verbatim on cartons).</summary>
    public string? ShippingMarks { get; set; }
    /// <summary>Total number of cartons / packages in the shipment.</summary>
    public int? TotalPackages { get; set; }
    /// <summary>Gross weight in kilograms (boxes included).</summary>
    public decimal? GrossWeightKg { get; set; }
    /// <summary>Net weight in kilograms (goods only).</summary>
    public decimal? NetWeightKg { get; set; }

    public ICollection<CustomerInvoiceLine> Lines { get; set; } = new List<CustomerInvoiceLine>();

    /// <summary>
    /// 1-to-1 VAT challan auto-created on Issue when <see cref="VatAmount"/> &gt; 0.
    /// Null until issued. Soft-deleted automatically if invoice is cancelled.
    /// </summary>
    public VatChallan? VatChallan { get; set; }
}

/// <summary>
/// A single product line on a <see cref="CustomerInvoice"/>. Editable while the parent is
/// in Draft state; locked once Issued.
/// </summary>
public class CustomerInvoiceLine : BaseTransactionalEntity
{
    public long CustomerInvoiceId { get; set; }
    public CustomerInvoice CustomerInvoice { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public int SortOrder { get; set; }

    public string? LineNotes { get; set; }
}

public enum CustomerInvoiceStatus
{
    Draft = 1,
    Issued = 2,
    PartiallyPaid = 3,    // 0 < AmountPaid < TotalAmount
    Paid = 4,             // AmountPaid >= TotalAmount
    Cancelled = 5         // only allowed when AmountPaid = 0
}
