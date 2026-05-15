namespace BengalTex.ERP.Application.Inventory.Dtos;

public record StockOnHandDto(
    int RawMaterialId,
    string RawMaterialCode,
    string RawMaterialName,
    string UnitOfMeasureCode,
    int WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    decimal Quantity,
    decimal MinimumStockLevel,
    bool BelowMinimum);                  // computed: Quantity < MinimumStockLevel

public record StockMovementDto(
    long Id,
    string Code,
    int RawMaterialId,
    string RawMaterialCode,
    string RawMaterialName,
    string UnitOfMeasureCode,
    int WarehouseId,
    string WarehouseCode,
    decimal SignedQuantity,              // + in, − out
    string MovementType,                 // enum as string
    string? ReferenceType,
    long? ReferenceId,
    string? ReferenceCode,
    DateOnly MovementDate,
    string? Notes,
    DateTimeOffset CreatedAt);

public record StockAdjustmentDto(
    long Id,
    string Code,
    DateOnly AdjustmentDate,
    int WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    string Reason,
    string Status,
    DateTimeOffset? PostedAt,
    string? PostedBy,
    string? Notes,
    IReadOnlyList<StockAdjustmentLineDto> Lines);

public record StockAdjustmentLineDto(
    long Id,
    int RawMaterialId,
    string RawMaterialCode,
    string RawMaterialName,
    string UnitOfMeasureCode,
    decimal SignedQuantity,
    int SortOrder,
    string? LineNotes);

public record StockAdjustmentListItemDto(
    long Id,
    string Code,
    DateOnly AdjustmentDate,
    int WarehouseId,
    string WarehouseName,
    string Reason,
    string Status,
    int LineCount);
