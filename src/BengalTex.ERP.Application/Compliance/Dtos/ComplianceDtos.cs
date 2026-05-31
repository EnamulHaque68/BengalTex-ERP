namespace BengalTex.ERP.Application.Compliance.Dtos;

public record ComplianceCertificateDto(
    int Id,
    string Name,
    string CertificateType,
    string? IssuingAuthority,
    string? CertificateNumber,
    DateOnly IssuedDate,
    DateOnly ExpiryDate,
    int DaysUntilExpiry,             // negative if already expired
    string ExpiryStatus,             // "Active" | "ExpiringSoon" | "Expired"
    string? Notes,
    bool IsActive);

public record AuditFindingDto(
    long Id,
    long ComplianceAuditId,
    string FindingDescription,
    string Severity,
    string? CorrectiveAction,
    int? AssignedToEmployeeId,
    string? AssignedToEmployeeName,
    DateOnly? DueDate,
    DateOnly? ClosureDate,
    string Status,
    bool IsOverdue,
    string? Notes);

public record ComplianceAuditDto(
    long Id,
    string Code,
    string AuditType,
    string Auditor,
    DateOnly ScheduledDate,
    DateOnly? ActualDate,
    string Status,
    string? Result,
    decimal? Score,
    string? Notes,
    IReadOnlyList<AuditFindingDto> Findings);

public record ComplianceAuditListItemDto(
    long Id,
    string Code,
    string AuditType,
    string Auditor,
    DateOnly ScheduledDate,
    DateOnly? ActualDate,
    string Status,
    string? Result,
    decimal? Score,
    int OpenFindings);

public record ComplianceDashboardDto(
    int CertificatesActive,
    int CertificatesExpiringSoon,    // ≤60d
    int CertificatesExpired,
    int OpenFindings,
    int OverdueFindings,
    int UpcomingAudits,              // next 30d
    IReadOnlyList<ComplianceCertificateDto> ExpiringCertificates,
    IReadOnlyList<AuditFindingDto> OverdueFindingsList);
