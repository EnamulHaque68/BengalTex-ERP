namespace BengalTex.ERP.Application.Company.Dtos;

public record CompanyDto(
    int Id,
    string Name,
    string ShortName,
    string? RegistrationNumber,
    string? TaxNumber,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string District,
    string? PostalCode,
    string Country,
    string? Phone,
    string? Email,
    string? Website,
    string? LogoUrl,
    bool IsActive
);
