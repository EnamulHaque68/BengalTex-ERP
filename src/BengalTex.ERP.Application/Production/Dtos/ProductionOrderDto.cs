namespace BengalTex.ERP.Application.Production.Dtos;

public record ProductionOrderDto(
    long Id,
    string Code,
    int ProductId,
    string ProductCode,
    string ProductName,
    string ProductUnitOfMeasureCode,
    int BomId,
    string BomCode,
    int BomVersion,
    decimal BomOutputQuantity,
    decimal Quantity,
    int IssueWarehouseId,
    string IssueWarehouseCode,
    string IssueWarehouseName,
    int ReceiveWarehouseId,
    string ReceiveWarehouseCode,
    string ReceiveWarehouseName,
    DateOnly? PlannedStartDate,
    DateOnly? PlannedEndDate,
    DateOnly? ActualStartDate,
    DateOnly? ActualEndDate,
    string Status,                       // enum as string
    DateTimeOffset? CompletedAt,
    string? CompletedBy,
    string? Notes,
    IReadOnlyList<ProductionPlannedLineDto> PlannedLines,
    IReadOnlyList<ProductionStageDto> Stages,
    // Phase 1 — source Sales Order link (null = standalone / manual run)
    long? SalesOrderId = null,
    string? SalesOrderCode = null,
    long? SalesOrderLineId = null,
    string? CustomerName = null,
    // Phase 5 — Quality Hold
    bool RequiresQc = false,
    DateTimeOffset? QcReleasedAt = null,
    bool QcHeld = false,                 // QcHeldQuantity > 0
    decimal QcHeldQuantity = 0m,         // remaining QC-held finished-goods qty (Active "QcHold" reservation)
    // Phase 6 — cost sheet (base BDT)
    decimal MaterialCost = 0m,           // auto at Complete
    decimal LabourCost = 0m,
    decimal MachineCost = 0m,
    decimal OverheadCost = 0m,
    decimal SubcontractCost = 0m,
    decimal WastageCost = 0m,
    decimal RejectCost = 0m,
    decimal TotalProductionCost = 0m,    // Σ all components
    decimal CostPerUnit = 0m);           // Total ÷ Quantity

/// <summary>One routing stage of a production order with its WIP progress + line/operator.</summary>
public record ProductionStageDto(
    long Id,
    int Sequence,
    string StageName,
    string Status,                       // Pending | InProgress | Completed | Skipped
    decimal PlannedQuantity,
    decimal CompletedQuantity,
    decimal RejectedQuantity,
    string? ProductionLine,
    int? OperatorEmployeeId,
    string? OperatorEmployeeName,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    string? Notes,
    // Phase 4 — resource assignment
    int? WorkCenterId = null,
    string? WorkCenterName = null,
    int? ShiftId = null,
    string? ShiftName = null);

/// <summary>
/// Computed material-consumption preview per BOM line — scaled to the production's output qty.
/// Polymorphic: each line is a raw material or a semi-finished component product (sub-assembly).
/// Includes current on-hand in the issue warehouse so the user sees up-front whether
/// completion will succeed (stock-sufficient) or fail.
/// </summary>
public record ProductionPlannedLineDto(
    string ItemType,                     // "RawMaterial" | "Product"
    int ItemId,                          // RM id or component-product id
    string ItemCode,
    string ItemName,
    string UnitOfMeasureCode,
    decimal BomLineQuantity,             // per the BOM's output batch
    decimal WastagePercent,
    decimal ScaledQuantity,              // bomLineQty × (1 + wastage%) × (production qty / bom output qty)
    decimal CurrentOnHand,               // in the IssueWarehouse
    bool Sufficient);                    // CurrentOnHand >= ScaledQuantity

public record ProductionOrderListItemDto(
    long Id,
    string Code,
    int ProductId,
    string ProductName,
    int BomVersion,
    decimal Quantity,
    string Status,
    DateOnly? PlannedStartDate,
    DateOnly? ActualEndDate,
    int StageCount,                      // routing length (0 = single-step)
    int CompletedStageCount,            // stages Completed/Skipped
    string? CurrentStageName,           // first non-finished stage, null when none/all done
    long? SalesOrderId,                 // Phase 1 — source SO (null = standalone)
    string? SalesOrderCode,
    bool RequiresQc,                    // Phase 5 — Quality Hold opt-in
    bool QcHeld,                        // QcHeldQuantity > 0
    decimal QcHeldQuantity);            // remaining QC-held finished-goods qty
