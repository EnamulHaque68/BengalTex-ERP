namespace BengalTex.ERP.Application.Reports.Dtos;

/// <summary>
/// Dead / slow-moving stock — items currently holding quantity that have had NO stock movement
/// for at least <see cref="DaysThreshold"/> days (as of <see cref="AsOfDate"/>). Helps surface
/// capital tied up in non-moving inventory. Value uses the item's current weighted-average cost.
/// </summary>
public record DeadStockReportDto(
    DateTimeOffset GeneratedAt,
    DateOnly AsOfDate,
    int DaysThreshold,
    int? WarehouseId,                  // echo of filter (null = all warehouses)
    string? WarehouseName,
    string? ItemType,                  // echo of filter ("RawMaterial" | "Product" | null = both)
    int RowCount,
    decimal TotalDeadValue,            // Σ (qty × WAC) of all dead rows, BDT
    IReadOnlyList<DeadStockRowDto> Rows);

public record DeadStockRowDto(
    string ItemType,                   // "RawMaterial" | "Product"
    int? RawMaterialId,
    int? ProductId,
    string Code,
    string Name,
    string UnitOfMeasureCode,
    decimal Quantity,                  // current on-hand (across warehouses, or in the filtered one)
    decimal UnitCost,                  // weighted-average cost
    decimal Value,                     // Quantity × UnitCost
    DateOnly? LastMovementDate,        // null = never moved (e.g. legacy opening with no movement row)
    int DaysSinceLastMovement);        // int.MaxValue-ish capped; large when never moved
