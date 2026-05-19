namespace BengalTex.ERP.Application.Reports.Dtos;

public record StockSummaryReportDto(
    DateTimeOffset GeneratedAt,
    int? WarehouseId,                  // echo of filter (null = all warehouses)
    string? WarehouseName,
    string? ItemType,                  // echo of filter ("RawMaterial" | "Product" | null = both)
    int RowCount,
    decimal TotalRawMaterialQuantity,
    decimal TotalProductQuantity,
    IReadOnlyList<StockSummaryRowDto> Rows);

public record StockSummaryRowDto(
    string ItemType,                   // "RawMaterial" | "Product"
    int? RawMaterialId,
    int? ProductId,
    string Code,
    string Name,
    string UnitOfMeasureCode,
    decimal TotalQuantity,             // summed across all warehouses (or in selected warehouse only)
    int WarehouseCount);               // how many warehouses currently hold this item
