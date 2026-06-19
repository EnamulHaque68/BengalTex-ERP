namespace BengalTex.ERP.Application.Customer.Dtos;

/// <summary>Full customer details for view/edit screens.</summary>
public record CustomerDto(
    int Id,
    string Code,
    string Name,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? Website,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string? District,
    string? PostalCode,
    string Country,
    string? BinNumber,
    string? VatNumber,
    string? TinNumber,
    string Category,            // Enum as string: "A", "B", "C"
    decimal CreditLimit,
    int CreditPeriodDays,
    bool IsExport,              // explicit "export buyer" flag (additive to currency heuristic)
    int? ParentCustomerId,      // head office (null = top-level)
    string? Notes,
    bool IsActive);

/// <summary>Compact customer for list/table views.</summary>
public record CustomerListItemDto(
    int Id,
    string Code,
    string Name,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string City,
    string Category,
    decimal CreditLimit,
    int CreditPeriodDays,
    bool IsExport,
    int? ParentCustomerId,
    bool IsActive);
