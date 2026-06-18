using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Go-live opening-stock seed document. Unlike <see cref="StockAdjustment"/> (raw-material only,
/// count-correction journal), this seeds BOTH raw materials AND finished products at a known
/// unit cost: Post sets each item's weighted-average cost, writes an
/// <see cref="StockMovementType.OpeningStock"/> movement, and posts ONE balanced journal
/// (Dr Raw Material / Finished Goods Inventory, Cr Opening Balance Equity). Posted entries are
/// immutable. This is the only path that originates finished-goods stock outside Production.
/// </summary>
public class OpeningStockEntry : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;

    public DateOnly EntryDate { get; set; }

    /// <summary>Warehouse the opening quantities land in (one entry per warehouse).</summary>
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    public OpeningStockStatus Status { get; set; } = OpeningStockStatus.Draft;

    public DateTimeOffset? PostedAt { get; set; }
    public string? PostedBy { get; set; }

    public string? Notes { get; set; }

    public ICollection<OpeningStockLine> Lines { get; set; } = new List<OpeningStockLine>();
}

/// <summary>
/// One opening-stock line — polymorphic item: exactly one of <see cref="RawMaterialId"/> /
/// <see cref="ProductId"/> is set (DB check constraint), same pattern as <see cref="StockMovement"/>.
/// </summary>
public class OpeningStockLine : BaseTransactionalEntity
{
    public long OpeningStockEntryId { get; set; }
    public OpeningStockEntry OpeningStockEntry { get; set; } = null!;

    public int? RawMaterialId { get; set; }
    public RawMaterial? RawMaterial { get; set; }

    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    /// <summary>Opening quantity on hand (positive).</summary>
    public decimal Quantity { get; set; }

    /// <summary>Known unit cost — seeds the item's weighted-average cost + inventory value.</summary>
    public decimal UnitCost { get; set; }

    public int SortOrder { get; set; }

    public string? LineNotes { get; set; }
}

public enum OpeningStockStatus
{
    Draft = 1,
    Posted = 2
}
