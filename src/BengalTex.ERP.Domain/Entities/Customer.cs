using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Customer master. System-wide unique code (auto-generated via NumberingService
/// when not supplied, or manually entered for legacy data migration).
/// Multi-factory companies share their customer list.
/// </summary>
public class Customer : BaseEntity
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

    // Bangladesh tax / compliance identifiers (all optional — many small buyers won't have them)
    public string? BinNumber { get; set; }    // VAT Business Identification Number
    public string? VatNumber { get; set; }
    public string? TinNumber { get; set; }    // Tax ID Number

    // Business terms
    public CustomerCategory Category { get; set; } = CustomerCategory.B;
    public decimal CreditLimit { get; set; }              // 0 = no credit allowed
    public int CreditPeriodDays { get; set; }             // 0 / 30 / 60 / 90, etc.

    /// <summary>
    /// True = this customer's invoices are considered export (Form-N / Commercial Invoice / Packing List
    /// flows apply regardless of payment currency). False = derived from invoice currency
    /// (any non-BDT invoice is treated as export). Use for: BDT-paid export buyers, re-export
    /// gateway middlemen, etc.
    /// </summary>
    public bool IsExport { get; set; }

    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Customer grade — A is highest, C is lowest. Drives credit decisions, pricing tier, etc.
/// </summary>
public enum CustomerCategory
{
    A = 1,
    B = 2,
    C = 3
}
