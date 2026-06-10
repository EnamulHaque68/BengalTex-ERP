using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Product master — the garments accessories the factory produces or stocks
/// (woven labels, hand tags, care labels, poly bags, etc.).
///
/// MVP keeps variant attributes (Size/Color/Material) as plain text fields;
/// a full variant table is a later enhancement.
/// </summary>
public class Product : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Specification { get; set; }

    public int ProductCategoryId { get; set; }
    public ProductCategory ProductCategory { get; set; } = null!;

    // Stock-keeping unit of measure (e.g., PCS for labels, ROLL for tape)
    public int UnitOfMeasureId { get; set; }
    public UnitOfMeasure UnitOfMeasure { get; set; } = null!;

    // Simple variant attributes (full variant matrix deferred)
    public string? Size { get; set; }
    public string? Color { get; set; }
    public string? Material { get; set; }

    /// <summary>BD HS (Harmonised System) code for export classification — required on EPB Form-N.</summary>
    public string? HsCode { get; set; }

    public decimal SalesPrice { get; set; }
    public decimal ReorderLevel { get; set; }

    /// <summary>
    /// Running weighted-average production cost per unit, in base currency.
    /// System-maintained: recomputed on every Production Complete from the cost of RM
    /// consumed (Σ consumedQty × RM WeightedAverageCost ÷ produced qty). Global across
    /// warehouses. 0 until the first production receipt. Used for finished-goods
    /// inventory valuation.
    /// </summary>
    public decimal WeightedAverageCost { get; set; }

    /// <summary>True = kept in stock; False = made-to-order only.</summary>
    public bool IsStockItem { get; set; } = true;

    public string? ImageUrl { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}
