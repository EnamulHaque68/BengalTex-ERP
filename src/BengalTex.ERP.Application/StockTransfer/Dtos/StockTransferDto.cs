namespace BengalTex.ERP.Application.StockTransfer.Dtos;

public record StockTransferDto(
    long Id,
    string Code,
    int SourceWarehouseId,
    string SourceWarehouseCode,
    string SourceWarehouseName,
    int DestinationWarehouseId,
    string DestinationWarehouseCode,
    string DestinationWarehouseName,
    DateOnly TransferDate,
    string Status,
    DateTimeOffset? PostedAt,
    string? PostedBy,
    string? Notes,
    IReadOnlyList<StockTransferLineDto> Lines);

public record StockTransferLineDto(
    long Id,
    string ItemType,                  // "RawMaterial" | "Product"
    int? RawMaterialId,
    int? ProductId,
    string ItemCode,
    string ItemName,
    string UnitOfMeasureCode,
    decimal Quantity,
    int SortOrder,
    string? LineNotes);

public record StockTransferListItemDto(
    long Id,
    string Code,
    int SourceWarehouseId,
    string SourceWarehouseName,
    int DestinationWarehouseId,
    string DestinationWarehouseName,
    DateOnly TransferDate,
    string Status,
    int LineCount,
    decimal TotalQuantity);
