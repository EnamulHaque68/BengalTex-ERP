using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Supplier / vendor master. Like Customer, system-wide unique code, NumberingService-
/// generated when not supplied. Carries bank details for payout routing.
/// </summary>
public class Supplier : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }

    // Address
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string? District { get; set; }
    public string? PostalCode { get; set; }
    public string Country { get; set; } = "Bangladesh";

    // Bangladesh tax / compliance identifiers
    public string? BinNumber { get; set; }
    public string? VatNumber { get; set; }
    public string? TinNumber { get; set; }

    // Business terms
    public int PaymentTermsDays { get; set; }    // 0 (advance) / 15 / 30 / 60 / 90

    // Banking for payouts
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankBranch { get; set; }
    public string? BankAccountHolderName { get; set; }

    // Supplier performance score 1..5 — drives sourcing decisions.
    public int Rating { get; set; }

    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}
