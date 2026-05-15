namespace BengalTex.ERP.Application.Bom.Dtos;

public record BomDto(
    int Id,
    string Code,
    int ProductId,
    string ProductCode,
    string ProductName,
    string ProductUnitOfMeasureCode,
    int Version,
    string? Name,
    decimal OutputQuantity,
    string Status,                       // enum as string
    bool IsActive,
    DateOnly? EffectiveDate,
    DateTimeOffset? ApprovedAt,
    string? ApprovedBy,
    string? Notes,
    decimal TotalMaterialCost,           // rolled up from lines
    decimal CostPerUnit,                 // TotalMaterialCost / OutputQuantity
    IReadOnlyList<BomLineDto> Lines);

public record BomLineDto(
    int Id,
    int RawMaterialId,
    string RawMaterialCode,
    string RawMaterialName,
    string UnitOfMeasureCode,
    decimal Quantity,
    decimal WastagePercent,
    decimal EffectiveQuantity,           // Quantity * (1 + WastagePercent/100)
    decimal StandardCost,                // raw material's per-unit cost
    decimal LineCost,                    // EffectiveQuantity * StandardCost
    int SortOrder,
    string? LineNotes);

public record BomListItemDto(
    int Id,
    string Code,
    int ProductId,
    string ProductName,
    int Version,
    string Status,
    bool IsActive,
    decimal OutputQuantity,
    int LineCount,
    decimal TotalMaterialCost);
