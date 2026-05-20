namespace BengalTex.ERP.Application.QuarantineDisposition.Dtos;

public record QuarantineDispositionDto(
    long Id,
    string Code,
    string DispositionType,             // "Release" | "Scrap"
    DateOnly DispositionDate,
    int QuarantineWarehouseId,
    string QuarantineWarehouseName,
    int? DestinationWarehouseId,
    string? DestinationWarehouseName,
    string Status,
    string? Reason,
    DateTimeOffset? PostedAt,
    string? PostedBy,
    string? Notes,
    IReadOnlyList<QuarantineDispositionLineDto> Lines);

public record QuarantineDispositionLineDto(
    long Id,
    string ItemType,                    // "RawMaterial" | "Product"
    int? RawMaterialId,
    int? ProductId,
    string ItemCode,
    string ItemName,
    string UnitOfMeasureCode,
    decimal Quantity,
    decimal AvailableInQuarantine,      // current stock on hand in the quarantine warehouse
    int SortOrder,
    string? LineNotes);

public record QuarantineDispositionListItemDto(
    long Id,
    string Code,
    string DispositionType,
    DateOnly DispositionDate,
    int QuarantineWarehouseId,
    string QuarantineWarehouseName,
    string? DestinationWarehouseName,
    string Status,
    int LineCount,
    decimal TotalQuantity);
