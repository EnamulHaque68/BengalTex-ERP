namespace BengalTex.ERP.Application.UnitOfMeasure.Dtos;

public record UnitOfMeasureDto(
    int Id,
    string Code,
    string Name,
    string Symbol,
    string UnitType,            // Enum value as string: "Count", "Weight", etc.
    int? BaseUnitId,
    string? BaseUnitCode,        // Resolved for display
    decimal ConversionFactor,
    bool IsActive);
