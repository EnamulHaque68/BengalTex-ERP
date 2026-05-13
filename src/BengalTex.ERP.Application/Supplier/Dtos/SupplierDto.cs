namespace BengalTex.ERP.Application.Supplier.Dtos;

public record SupplierDto(
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
    int PaymentTermsDays,
    string? BankName,
    string? BankAccountNumber,
    string? BankBranch,
    string? BankAccountHolderName,
    int Rating,
    string? Notes,
    bool IsActive);

public record SupplierListItemDto(
    int Id,
    string Code,
    string Name,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string City,
    int PaymentTermsDays,
    int Rating,
    bool IsActive);
