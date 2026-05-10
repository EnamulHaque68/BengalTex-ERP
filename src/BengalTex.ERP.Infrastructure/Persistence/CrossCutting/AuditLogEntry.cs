using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Infrastructure.Persistence.CrossCutting;

/// <summary>
/// Granular change log. One row per changed entity per save.
/// Old/new values stored as JSON for diff comparison.
/// </summary>
public class AuditLogEntry : BaseTransactionalEntity
{
    public string EntityType { get; set; } = string.Empty;
    public string EntityKey { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;     // Insert/Update/Delete
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public string? AffectedColumns { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}