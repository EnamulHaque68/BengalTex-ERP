using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Buyer style / design — a buyer's specific accessory design and its spec (e.g. a woven
/// label or hangtag design for a particular customer/season). Links a <see cref="Customer"/>
/// (the buyer) and optionally the <see cref="Product"/> it is produced as.
/// </summary>
public class Style : BaseEntity
{
    public string Code { get; set; } = string.Empty;     // STY series
    public string StyleName { get; set; } = string.Empty;

    public int BuyerId { get; set; }                      // a Customer
    public Customer Buyer { get; set; } = null!;

    public int? ProductId { get; set; }                   // the accessory it maps to (optional)
    public Product? Product { get; set; }

    /// <summary>The buyer's own style/article number.</summary>
    public string? BuyerStyleRef { get; set; }
    public string? Season { get; set; }                   // e.g. "Summer 2026"

    public StyleStatus Status { get; set; } = StyleStatus.Development;

    public string? Description { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public enum StyleStatus
{
    Development = 1,
    Approved = 2,
    Running = 3,
    Discontinued = 4
}
