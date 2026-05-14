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
    string? PreferredSupplierName,
    bool IsActive);
