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
    IReadOnlyList<ProductionPlannedLineDto> PlannedLines);

/// <summary>
/// Computed RM consumption preview per BOM line — scaled to the production's output qty.
/// Includes current on-hand in the issue warehouse so the user sees up-front whether
/// completion will succeed (stock-sufficient) or fail.
/// </summary>
public record ProductionPlannedLineDto(
    int RawMaterialId,
    string RawMaterialCode,
    string RawMaterialName,
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
    DateOnly? ActualEndDate);
