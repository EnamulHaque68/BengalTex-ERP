using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Any compliance / licensing certificate held by the company — buyer-driven audits
/// (BSCI/Sedex/WRAP/SA8000) plus statutory BD licenses (Trade/Fire/Factory/Bond).
/// Expiry status is derived in queries (Active / ExpiringSoon (≤60d) / Expired).
/// Attach the certificate PDF via the existing polymorphic Attachment system
/// (entityType="ComplianceCertificate").
/// </summary>
public class ComplianceCertificate : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public ComplianceCertificateType CertificateType { get; set; }

    public string? IssuingAuthority { get; set; }
    public string? CertificateNumber { get; set; }

    public DateOnly IssuedDate { get; set; }
    public DateOnly ExpiryDate { get; set; }

    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public enum ComplianceCertificateType
{
    BSCI = 1,
    Sedex = 2,
    WRAP = 3,
    SA8000 = 4,
    ISO9001 = 5,
    ISO14001 = 6,
    TradeLicense = 7,
    FireLicense = 8,
    FactoryLicense = 9,
    EnvironmentClearance = 10,
    BondLicense = 11,
    BoilerCertificate = 12,
    OEKO_TEX = 13,
    GOTS = 14,
    Other = 99
}
