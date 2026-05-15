using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Manual stock correction document — opening-stock seed, year-end stock-take variance,
/// damage write-off, etc. Draft is editable; Post creates one <see cref="StockMovement"/>
/// per line and updates the corresponding <see cref="StockOnHand"/> rows atomically.
/// Posted adjustments are immutable.
/// </summary>
public class StockAdjustment : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;

    public DateOnly AdjustmentDate { get; set; }

    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    /// <summary>Short justification: 'Stock-take variance', 'Damaged goods write-off', etc.</summary>
    public string Reason { get; set; } = string.Empty;

    public StockAdjustmentStatus Status { get; set; } = StockAdjustmentStatus.Draft;

    public DateTimeOffset? PostedAt { get; set; }
    public string? PostedBy { get; set; }

    public string? Notes { get; set; }

    public ICollection<StockAdjustmentLine> Lines { get; set; } = new List<StockAdjustmentLine>();
}

/// <summary>One raw-material line on a <see cref="StockAdjustment"/>.</summary>
public class StockAdjustmentLine : BaseTransactionalEntity
{
    public long StockAdjustmentId { get; set; }
    public StockAdjustment StockAdjustment { get; set; } = null!;

    public int RawMaterialId { get; set; }
    public RawMaterial RawMaterial { get; set; } = null!;

    /// <summary>Positive (add stock) or negative (remove stock).</summary>
    public decimal SignedQuantity { get; set; }

    public int SortOrder { get; set; }

    public string? LineNotes { get; set; }
}

public enum StockAdjustmentStatus
{
    Draft = 1,
    Posted = 2
}
