using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Append-only audit record of a single stock change. Sums of <see cref="SignedQuantity"/>
/// over all movements for a (RawMaterialId, WarehouseId) pair equal that pair's
/// <see cref="StockOnHand.Quantity"/>. Transactional (long key) — high volume.
///
/// Every inbound (GRN receipt, adjustment-in, opening stock) and outbound (adjustment-out,
/// future issue/dispatch) writes a row here. Movements are never updated or deleted.
/// </summary>
public class StockMovement : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;

    public int RawMaterialId { get; set; }
    public RawMaterial RawMaterial { get; set; } = null!;

    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    /// <summary>Positive for inbound, negative for outbound.</summary>
    public decimal SignedQuantity { get; set; }

    public StockMovementType MovementType { get; set; }

    /// <summary>Source document kind, e.g. "GRN", "Adjustment", "Production".</summary>
    public string? ReferenceType { get; set; }

    /// <summary>Source document long id (nullable for opening stock with no source doc).</summary>
    public long? ReferenceId { get; set; }

    /// <summary>Source document display code, e.g. "BTX/GRN/2026/00001".</summary>
    public string? ReferenceCode { get; set; }

    public DateOnly MovementDate { get; set; }

    public string? Notes { get; set; }
}

public enum StockMovementType
{
    OpeningStock = 1,       // inbound, manual seed
    GrnReceipt = 2,         // inbound, from a posted GRN
    AdjustmentIn = 3,       // inbound, manual stock-take + adjustment
    AdjustmentOut = 4,      // outbound, manual write-off / correction
    ProductionIssue = 5,    // outbound, raw material consumed by Production (future)
    ProductionReceipt = 6,  // inbound, finished goods from Production (future)
    SalesDispatch = 7,      // outbound, dispatched to customer via DN (future)
    TransferIn = 8,         // inbound side of inter-warehouse transfer (future)
    TransferOut = 9         // outbound side of inter-warehouse transfer (future)
}
