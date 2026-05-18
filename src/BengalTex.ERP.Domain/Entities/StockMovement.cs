using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Append-only audit record of a single stock change. Sums of <see cref="SignedQuantity"/>
/// over all movements for an (item, WarehouseId) pair equal that pair's
/// <see cref="StockOnHand.Quantity"/>. Transactional (long key) — high volume.
///
/// Polymorphic item: exactly one of <see cref="RawMaterialId"/> / <see cref="ProductId"/>
/// is set per row, enforced by a DB check constraint. RawMaterial movements come from
/// GRN / Adjustment / Production-Issue; Product movements come from Production-Receipt
/// / Sales-Dispatch (future Delivery Note).
///
/// Every inbound (GRN, adjustment-in, opening stock, production-receipt) and outbound
/// (adjustment-out, production-issue, future sales-dispatch) writes a row here.
/// Movements are never updated or deleted.
/// </summary>
public class StockMovement : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;

    // ── Polymorphic item — exactly one of these is non-null (DB check constraint) ──
    public int? RawMaterialId { get; set; }
    public RawMaterial? RawMaterial { get; set; }

    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    /// <summary>Positive for inbound, negative for outbound.</summary>
    public decimal SignedQuantity { get; set; }

    public StockMovementType MovementType { get; set; }

    /// <summary>Source document kind, e.g. "GRN", "Adjustment", "ProductionOrder".</summary>
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
    ProductionIssue = 5,    // outbound, raw material consumed by Production
    ProductionReceipt = 6,  // inbound, finished goods from Production
    SalesDispatch = 7,      // outbound, dispatched to customer via DN (future)
    TransferIn = 8,         // inbound side of inter-warehouse transfer (future)
    TransferOut = 9         // outbound side of inter-warehouse transfer (future)
}
