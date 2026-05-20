namespace BengalTex.ERP.Application.Product.Dtos;

public record ProductDto(
    int Id,
    string Code,
    string Name,
    string? Specification,
    int ProductCategoryId,
    string ProductCategoryName,
    int UnitOfMeasureId,
    string UnitOfMeasureCode,
    string? Size,
    string? Color,
    string? Material,
    decimal SalesPrice,
    decimal ReorderLevel,
    decimal WeightedAverageCost,     // system-maintained production cost (Phase 14)
    bool IsStockItem,
    string? ImageUrl,
    string? Notes,
    bool IsActive);

public record ProductListItemDto(
    int Id,
    string Code,
    string Name,
    string ProductCategoryName,
    string UnitOfMeasureCode,
    decimal SalesPrice,
    decimal ReorderLevel,
    decimal WeightedAverageCost,
    bool IsStockItem,
    bool IsActive);
