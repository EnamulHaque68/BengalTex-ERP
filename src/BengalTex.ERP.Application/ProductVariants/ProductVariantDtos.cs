namespace BengalTex.ERP.Application.ProductVariants;

public sealed record ProductVariantDto(
    int Id,
    int ProductId,
    string VariantCode,
    string? Name,
    string? Color,
    string? Size,
    string? Sku,
    decimal? SalesPriceOverride,
    decimal EffectiveSalesPrice,     // SalesPriceOverride ?? product.SalesPrice
    string? Notes,
    bool IsActive);
