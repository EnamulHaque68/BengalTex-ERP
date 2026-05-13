namespace BengalTex.ERP.Application.Warehouse.Dtos;

public record WarehouseDto(
    int Id,
    string Code,
    string Name,
    string WarehouseType,        // Enum value as string
    string? Address,
    int FactoryId,
    string? FactoryName,         // Resolved for display
    bool IsActive);
