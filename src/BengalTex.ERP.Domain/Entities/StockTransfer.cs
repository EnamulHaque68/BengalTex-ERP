using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Inter-warehouse stock movement document — moves quantity of items (RawMaterial
/// or Product) from <see cref="SourceWarehouseId"/> to
/// <see cref="DestinationWarehouseId"/>. Lifecycle:
/// Draft → Posted (immutable; stock has shifted). Posting writes TWO stock
/// movements per line via <c>IStockService</c> — TransferOut at the source and
/// TransferIn at the destination — using the two-pass atomic pattern (validate
/// all lines for source stock availability, then apply all).
///
/// Each line is polymorphic: exactly one of <see cref="StockTransferLine.RawMaterialId"/>
/// / <see cref="StockTransferLine.ProductId"/> is set. A single transfer may mix
/// RM and Product lines (rare in practice but cheap to support).
///
/// No Cancel action — Draft transfers are simply deletable (soft delete); Posted
/// transfers are irreversible (post a counter-transfer to correct).
/// Transactional (long key) — unbounded volume.
/// </summary>
public class StockTransfer : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;

    public int SourceWarehouseId { get; set; }
    public Warehouse SourceWarehouse { get; set; } = null!;

    public int DestinationWarehouseId { get; set; }
    public Warehouse DestinationWarehouse { get; set; } = null!;

    public DateOnly TransferDate { get; set; }

    public StockTransferStatus Status { get; set; } = StockTransferStatus.Draft;

    public DateTimeOffset? PostedAt { get; set; }
    public string? PostedBy { get; set; }

    public string? Notes { get; set; }

    public ICollection<StockTransferLine> Lines { get; set; } = new List<StockTransferLine>();
}

/// <summary>
/// A single line on a <see cref="StockTransfer"/>: a quantity of one item
/// (RawMaterial or Product) being moved. Polymorphic — exactly one of
/// <see cref="RawMaterialId"/> / <see cref="ProductId"/> is set (enforced by a
/// DB check constraint, same pattern as <c>StockOnHand</c> and
/// <c>StockMovement</c>).
/// </summary>
public class StockTransferLine : BaseTransactionalEntity
{
    public long StockTransferId { get; set; }
    public StockTransfer StockTransfer { get; set; } = null!;

    // ── Polymorphic item — exactly one of these is non-null (DB check constraint) ──
    public int? RawMaterialId { get; set; }
    public RawMaterial? RawMaterial { get; set; }

    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    public decimal Quantity { get; set; }

    public int SortOrder { get; set; }

    public string? LineNotes { get; set; }
}

public enum StockTransferStatus
{
    Draft = 1,      // editable, no stock impact yet
    Posted = 2      // immutable, stock moved
}
