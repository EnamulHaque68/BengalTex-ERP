namespace BengalTex.ERP.Application.OpeningStock.Dtos;

public sealed record OpeningStockLineInput(
    string ItemType,        // "RawMaterial" | "Product"
    int ItemId,
    decimal Quantity,
    decimal UnitCost,
    string? LineNotes);

public sealed record OpeningStockLineDto(
    long Id,
    string ItemType,
    int? RawMaterialId,
    int? ProductId,
    string ItemCode,
    string ItemName,
    string UnitOfMeasureCode,
    decimal Quantity,
    decimal UnitCost,
    decimal LineValue,      // Quantity × UnitCost
    int SortOrder,
    string? LineNotes);

public sealed record OpeningStockEntryDto(
    long Id,
    string Code,
    DateOnly EntryDate,
    int WarehouseId,
    string WarehouseName,
    string Status,
    DateTimeOffset? PostedAt,
    string? PostedBy,
    string? Notes,
    decimal TotalValue,
    IReadOnlyList<OpeningStockLineDto> Lines);

public sealed record OpeningStockListItemDto(
    long Id,
    string Code,
    DateOnly EntryDate,
    string WarehouseCode,
    string Status,
    int LineCount,
    decimal TotalValue);
