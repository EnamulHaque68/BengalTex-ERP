namespace BengalTex.ERP.Application.StockLots.Dtos;

/// <summary>A traceable stock lot/batch with its current balance and source.</summary>
public record StockLotDto(
    long Id,
    string Code,
    string LotNumber,
    string ItemType,                 // "RawMaterial" | "Product"
    int ItemId,
    string ItemCode,
    string ItemName,
    string UnitOfMeasureCode,
    int WarehouseId,
    string WarehouseName,
    int? SupplierId,
    string? SupplierName,
    string? Shade,
    DateOnly ReceivedDate,
    DateOnly? ManufactureDate,
    DateOnly? ExpiryDate,
    decimal InitialQuantity,
    decimal CurrentQuantity,
    string Status,                   // LotStatus as string
    bool IsExpired,                  // derived: ExpiryDate < today
    string? SourceType,
    long? SourceId,
    string? SourceCode,
    string? Notes);

/// <summary>A single movement tagged to a lot — the lot's traceability trail.</summary>
public record StockLotMovementDto(
    long Id,
    string Code,
    string MovementType,
    decimal SignedQuantity,
    DateOnly MovementDate,
    string? ReferenceType,
    string? ReferenceCode,
    string? Notes);

/// <summary>A lot plus the movements tagged to it (detail view).</summary>
public record StockLotDetailDto(
    StockLotDto Lot,
    IReadOnlyList<StockLotMovementDto> Movements);
