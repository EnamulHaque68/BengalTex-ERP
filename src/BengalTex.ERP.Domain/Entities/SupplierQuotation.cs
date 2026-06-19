using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// A supplier's price quotation (RFQ response) for a set of raw materials — optionally against a
/// <see cref="PurchaseRequisition"/>. Procurement collects several suppliers' quotations, compares
/// them side-by-side (converted to base currency), then selects the winner, which converts into a
/// <see cref="PurchaseOrder"/>. Quote amounts are in the quotation's own currency; the base value
/// = amount × <see cref="ExchangeRate"/>.
/// </summary>
public class SupplierQuotation : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;

    public DateOnly QuotationDate { get; set; }

    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    /// <summary>Optional requisition this quotes against — groups competing quotes for comparison.</summary>
    public long? PurchaseRequisitionId { get; set; }
    public PurchaseRequisition? PurchaseRequisition { get; set; }

    public int CurrencyId { get; set; }
    public Currency Currency { get; set; } = null!;

    /// <summary>Quotation currency → base (BDT) rate. Base amount = amount × this.</summary>
    public decimal ExchangeRate { get; set; } = 1m;

    /// <summary>Quote validity — informational.</summary>
    public DateOnly? ValidUntil { get; set; }

    public SupplierQuotationStatus Status { get; set; } = SupplierQuotationStatus.Draft;

    public DateTimeOffset? DecidedAt { get; set; }
    public string? DecidedBy { get; set; }

    /// <summary>Set when this quotation is selected and converted to a purchase order.</summary>
    public long? ConvertedPurchaseOrderId { get; set; }
    public DateTimeOffset? ConvertedAt { get; set; }

    public string? Notes { get; set; }

    public ICollection<SupplierQuotationLine> Lines { get; set; } = new List<SupplierQuotationLine>();
}

public class SupplierQuotationLine : BaseTransactionalEntity
{
    public long SupplierQuotationId { get; set; }
    public SupplierQuotation SupplierQuotation { get; set; } = null!;

    public int RawMaterialId { get; set; }
    public RawMaterial RawMaterial { get; set; } = null!;

    public decimal Quantity { get; set; }

    /// <summary>Quoted unit price in the quotation's currency.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Quoted delivery lead time in days — informational, aids supplier selection.</summary>
    public int? LeadTimeDays { get; set; }

    public int SortOrder { get; set; }
    public string? LineNotes { get; set; }
}

public enum SupplierQuotationStatus
{
    Draft = 1,
    Submitted = 2,
    Selected = 3,
    Rejected = 4
}
