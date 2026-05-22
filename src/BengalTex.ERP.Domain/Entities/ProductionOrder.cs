using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// A production run — uses a <see cref="Bom"/> recipe to consume raw materials from
/// the issue warehouse and produce finished <see cref="Product"/> units into the
/// receive warehouse. Lifecycle: Draft → InProgress → Completed, plus Cancelled.
///
/// <see cref="BomId"/> is a snapshot — even if the active BOM for the product changes
/// after creation, this production uses the version selected here.
///
/// All stock impact happens atomically at Complete: ProductionIssue movements for
/// each BOM line (RM out) + a ProductionReceipt movement (Product in).
/// </summary>
public class ProductionOrder : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    /// <summary>The BOM version this production uses (snapshot at create time).</summary>
    public int BomId { get; set; }
    public Bom Bom { get; set; } = null!;

    /// <summary>How many finished units to produce.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Warehouse the raw materials are issued from.</summary>
    public int IssueWarehouseId { get; set; }
    public Warehouse IssueWarehouse { get; set; } = null!;

    /// <summary>Warehouse the finished goods are received into.</summary>
    public int ReceiveWarehouseId { get; set; }
    public Warehouse ReceiveWarehouse { get; set; } = null!;

    public DateOnly? PlannedStartDate { get; set; }
    public DateOnly? PlannedEndDate { get; set; }
    public DateOnly? ActualStartDate { get; set; }
    public DateOnly? ActualEndDate { get; set; }

    public ProductionOrderStatus Status { get; set; } = ProductionOrderStatus.Draft;

    public DateTimeOffset? CompletedAt { get; set; }
    public string? CompletedBy { get; set; }

    public string? Notes { get; set; }

    /// <summary>
    /// Optional multi-stage routing (Cutting → Sewing → Finishing → Packing …). Additive:
    /// an order with no stages behaves exactly as a single-step run. When stages exist, every
    /// stage must be Completed/Skipped before the order can be completed. Stages track WIP
    /// progress + line/operator only — all stock impact still happens at order Complete.
    /// </summary>
    public ICollection<ProductionStage> Stages { get; set; } = new List<ProductionStage>();
}

/// <summary>
/// One step in a production order's routing. Tracks the quantity worked through this stage,
/// the line/operator who did it, and timing. No stock movement of its own (v1).
/// </summary>
public class ProductionStage : BaseTransactionalEntity
{
    public long ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;

    /// <summary>1-based position in the routing.</summary>
    public int Sequence { get; set; }

    /// <summary>e.g. "Cutting", "Sewing", "Finishing", "Packing".</summary>
    public string StageName { get; set; } = string.Empty;

    public ProductionStageStatus Status { get; set; } = ProductionStageStatus.Pending;

    /// <summary>Units expected to pass through this stage (defaults to the order quantity).</summary>
    public decimal PlannedQuantity { get; set; }

    /// <summary>Good units that completed this stage.</summary>
    public decimal CompletedQuantity { get; set; }

    /// <summary>Units rejected / scrapped at this stage.</summary>
    public decimal RejectedQuantity { get; set; }

    /// <summary>Production line / workstation, free text.</summary>
    public string? ProductionLine { get; set; }

    /// <summary>Operator assigned to this stage (reuses the Employee master).</summary>
    public int? OperatorEmployeeId { get; set; }
    public Employee? OperatorEmployee { get; set; }

    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public string? Notes { get; set; }
}

public enum ProductionOrderStatus
{
    Draft = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}

public enum ProductionStageStatus
{
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    Skipped = 4
}
