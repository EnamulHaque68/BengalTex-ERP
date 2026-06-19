using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Sale of reusable scrap / wastage (paper trim, fabric waste, defective rejects sold for recovery).
/// Financial-only — scrap isn't carried as stock, so Post just records the income: one balanced
/// journal Dr Cash/Bank, Cr Scrap Sales Income. Posted sales are immutable.
/// </summary>
public class ScrapSale : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;

    public DateOnly SaleDate { get; set; }

    /// <summary>Who bought the scrap (free text — usually an informal scrap dealer).</summary>
    public string? BuyerName { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    public ScrapSaleStatus Status { get; set; } = ScrapSaleStatus.Draft;

    public DateTimeOffset? PostedAt { get; set; }
    public string? PostedBy { get; set; }

    public string? Notes { get; set; }

    public ICollection<ScrapSaleLine> Lines { get; set; } = new List<ScrapSaleLine>();
}

public class ScrapSaleLine : BaseTransactionalEntity
{
    public long ScrapSaleId { get; set; }
    public ScrapSale ScrapSale { get; set; } = null!;

    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal UnitPrice { get; set; }
    public int SortOrder { get; set; }
}

public enum ScrapSaleStatus
{
    Draft = 1,
    Posted = 2
}
