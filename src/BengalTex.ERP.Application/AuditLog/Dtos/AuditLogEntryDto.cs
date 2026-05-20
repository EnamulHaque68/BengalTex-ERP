namespace BengalTex.ERP.Application.AuditLog.Dtos;

public record AuditLogEntryDto(
    long Id,
    string EntityType,
    string EntityKey,
    string Action,                  // Insert | Update | Delete
    string? UserName,
    string? IpAddress,
    string? AffectedColumns,
    string? OldValuesJson,
    string? NewValuesJson,
    DateTimeOffset Timestamp);
