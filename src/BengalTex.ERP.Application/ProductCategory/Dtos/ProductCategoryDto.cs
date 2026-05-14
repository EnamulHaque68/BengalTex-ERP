namespace BengalTex.ERP.Application.ProductCategory.Dtos;

public record ProductCategoryDto(
    int Id,
    string Code,
    string Name,
    string? Description,
    int ProductCount,
    bool IsActive);
