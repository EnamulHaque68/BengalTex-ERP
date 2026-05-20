namespace BengalTex.ERP.Application.RawMaterial.Dtos;

public record RawMaterialDto(
    int Id,
    string Code,
    string Name,
    string? Specification,
    string Category,                 // enum as string
    int UnitOfMeasureId,
    string UnitOfMeasureCode,
    decimal MinimumStockLevel,
    decimal OpeningStock,
    decimal StandardCost,
    decimal WeightedAverageCost,     // system-maintained actual cost (Phase 14)
    int? PreferredSupplierId,
    string? PreferredSupplierName,
    string? Notes,
    bool IsActive);

public record RawMaterialListItemDto(
    int Id,
    string Code,
    string Name,
    string Category,
    string UnitOfMeasureCode,
    decimal MinimumStockLevel,
    decimal StandardCost,
    decimal WeightedAverageCost,
    string? PreferredSupplierName,
    bool IsActive);
