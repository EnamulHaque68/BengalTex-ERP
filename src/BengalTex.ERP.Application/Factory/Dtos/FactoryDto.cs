namespace BengalTex.ERP.Application.Factory.Dtos;

public record FactoryDto(
    int Id,
    string Name,
    string Code,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string? District,
    string? PostalCode,
    string? Phone,
    bool IsActive,
    double? GeoFenceLat,
    double? GeoFenceLng,
    double? GeoFenceRadiusMeters,
    int CompanyId
);

public record FactoryListItemDto(
    int Id,
    string Name,
    string Code,
    string City,
    string? Phone,
    bool IsActive,
    bool HasGeoFence
);
