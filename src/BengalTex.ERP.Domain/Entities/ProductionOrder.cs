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
}

public enum ProductionOrderStatus
{
    Draft = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}
