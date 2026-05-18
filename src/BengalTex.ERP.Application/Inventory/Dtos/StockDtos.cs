namespace BengalTex.ERP.Application.Inventory.Dtos;

/// <summary>
/// Generic stock snapshot row — covers both RawMaterial and Product (polymorphic).
/// <see cref="ItemType"/> is either "RawMaterial" or "Product"; the matching id field
/// (RawMaterialId / ProductId) is non-null.
/// </summary>
public record StockOnHandDto(
    string ItemType,                     // "RawMaterial" | "Product"
    int? RawMaterialId,
    int? ProductId,
    string ItemCode,                     // resolved code (RM or Product)
    string ItemName,                     // resolved name
    string UnitOfMeasureCode,
    int WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    decimal Quantity,
    decimal MinimumStockLevel,           // 0 for Product rows (no min-stock concept on Product)
    bool BelowMinimum);                  // false for Product rows

public record StockMovementDto(
    long Id,
    string Code,
    string ItemType,                     // "RawMaterial" | "Product"
    int? RawMaterialId,
    int? ProductId,
    string ItemCode,
    string ItemName,
    string UnitOfMeasureCode,
    int WarehouseId,
    string WarehouseCode,
    decimal SignedQuantity,              // + in, − out
    string MovementType,
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
