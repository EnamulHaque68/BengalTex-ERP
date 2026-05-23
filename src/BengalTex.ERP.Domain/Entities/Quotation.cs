using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// A pre-order price quotation to a customer with a built-in costing calculator per line
/// (material + labor + machine + overhead, scaled by wastage %, marked up by margin % → unit
/// price). Lifecycle: Draft → Sent → Accepted | Rejected | Expired; an Accepted quotation can be
/// converted into a Sales Order. "Revise" clones a quotation into a new version
/// (<see cref="RevisionOfId"/> + <see cref="Version"/>) so negotiations are tracked.
/// Multi-currency like Sales Orders (amounts in the quote currency; base BDT = amount × rate).
/// </summary>
public class Quotation : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public DateOnly QuotationDate { get; set; }
    public DateOnly? ValidUntil { get; set; }

    public int CurrencyId { get; set; }
    public Currency Currency { get; set; } = null!;
    public decimal ExchangeRate { get; set; } = 1m;

    public QuotationStatus Status { get; set; } = QuotationStatus.Draft;

    /// <summary>Revision chain — the quotation this one supersedes (null for the first version).</summary>
    public long? RevisionOfId { get; set; }
    public Quotation? RevisionOf { get; set; }
    public int Version { get; set; } = 1;

    /// <summary>Sum of line totals (selling), in the quote currency.</summary>
    public decimal TotalAmount { get; set; }

    public string? CustomerReference { get; set; }
    public string? Notes { get; set; }

    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    public string? DecidedBy { get; set; }

    /// <summary>Set when converted — the resulting Sales Order id.</summary>
    public long? ConvertedSalesOrderId { get; set; }

    public ICollection<QuotationLine> Lines { get; set; } = new List<QuotationLine>();
}

/// <summary>
/// One quoted product with its per-unit cost breakdown. Computed on save:
///   UnitCost  = (Material + Labor + Machine + Overhead) × (1 + Wastage%/100)
///   UnitPrice = UnitCost × (1 + Margin%/100)
///   LineTotal = UnitPrice × Quantity
/// </summary>
public class QuotationLine : BaseTransactionalEntity
{
    public long QuotationId { get; set; }
    public Quotation Quotation { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public string? Description { get; set; }
    public decimal Quantity { get; set; }

    // ── Per-unit cost components ──
    public decimal MaterialCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal MachineCost { get; set; }
    public decimal OverheadCost { get; set; }
    public decimal WastagePercent { get; set; }
    public decimal MarginPercent { get; set; }

    // ── Computed (denormalized on save) ──
    public decimal UnitCost { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    public int SortOrder { get; set; }
}

public enum QuotationStatus
{
    Draft = 1,
    Sent = 2,
    Accepted = 3,
    Rejected = 4,
    Expired = 5,
    Converted = 6     // accepted + turned into a Sales Order
}
