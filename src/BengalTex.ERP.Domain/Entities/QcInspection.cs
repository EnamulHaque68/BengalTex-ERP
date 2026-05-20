using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Quality-control inspection of a received or produced lot. Two sources:
///   - <see cref="QcSourceType.IncomingMaterial"/> against a posted
///     <see cref="GoodsReceiptNote"/> (incoming raw-material inspection);
///   - <see cref="QcSourceType.FinishedGoods"/> against a completed
///     <see cref="ProductionOrder"/> (finished-goods inspection).
///
/// Posting routes the REJECTED quantity per line out of the inspected (source)
/// warehouse into a <see cref="QuarantineWarehouseId"/> via paired QcRejectOut +
/// QcRejectIn stock movements — segregating failed material so it's no longer usable.
/// Passed quantity is untouched (stays in the source warehouse).
///
/// Lifecycle: Draft → Posted (immutable). No Cancel — post a counter / inventory
/// adjustment to correct. Transactional (long key).
/// </summary>
public class QcInspection : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;

    public QcSourceType SourceType { get; set; }

    /// <summary>Set when <see cref="SourceType"/> = IncomingMaterial.</summary>
    public long? GoodsReceiptNoteId { get; set; }
    public GoodsReceiptNote? GoodsReceiptNote { get; set; }

    /// <summary>Set when <see cref="SourceType"/> = FinishedGoods.</summary>
    public long? ProductionOrderId { get; set; }
    public ProductionOrder? ProductionOrder { get; set; }

    public DateOnly InspectionDate { get; set; }

    /// <summary>Warehouse the inspected stock currently sits in (GRN receiving / Production receive wh).</summary>
    public int InspectedFromWarehouseId { get; set; }
    public Warehouse InspectedFromWarehouse { get; set; } = null!;

    /// <summary>Warehouse rejected stock is routed to (held out of usable inventory).</summary>
    public int QuarantineWarehouseId { get; set; }
    public Warehouse QuarantineWarehouse { get; set; } = null!;

    public QcInspectionStatus Status { get; set; } = QcInspectionStatus.Draft;

    /// <summary>Computed on Post: all-passed → Passed, all-rejected → Failed, mixed → PartiallyPassed.</summary>
    public QcResult OverallResult { get; set; } = QcResult.Passed;

    public string? InspectedBy { get; set; }

    public string? Notes { get; set; }

    public ICollection<QcInspectionLine> Lines { get; set; } = new List<QcInspectionLine>();
}

/// <summary>
/// A single inspected item line. Polymorphic — exactly one of <see cref="RawMaterialId"/>
/// / <see cref="ProductId"/> set (RM for IncomingMaterial source, Product for FinishedGoods),
/// enforced by a DB check constraint.
/// </summary>
public class QcInspectionLine : BaseTransactionalEntity
{
    public long QcInspectionId { get; set; }
    public QcInspection QcInspection { get; set; } = null!;

    public int? RawMaterialId { get; set; }
    public RawMaterial? RawMaterial { get; set; }

    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    public decimal InspectedQuantity { get; set; }
    public decimal PassedQuantity { get; set; }

    /// <summary>InspectedQuantity − PassedQuantity. Routed to the quarantine warehouse on Post.</summary>
    public decimal RejectedQuantity { get; set; }

    public int SortOrder { get; set; }

    public string? DefectNotes { get; set; }
}

public enum QcSourceType
{
    IncomingMaterial = 1,   // against a GRN — RawMaterial lines
    FinishedGoods = 2       // against a Production Order — Product lines
}

public enum QcInspectionStatus
{
    Draft = 1,
    Posted = 2
}

public enum QcResult
{
    Passed = 1,             // nothing rejected
    PartiallyPassed = 2,    // some passed, some rejected
    Failed = 3              // everything rejected
}
