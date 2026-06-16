using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// An append-only record of a change to a <see cref="Product"/>'s sales price. One row is written
/// each time the price actually changes (on Product update). The audit fields (CreatedAt/CreatedBy)
/// carry when and by whom — so there is no separate "changed at / changed by".
/// </summary>
public class ProductPriceHistory : BaseEntity
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public decimal OldPrice { get; set; }
    public decimal NewPrice { get; set; }
}
