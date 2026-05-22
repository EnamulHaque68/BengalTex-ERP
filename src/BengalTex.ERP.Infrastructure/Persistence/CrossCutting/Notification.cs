using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Infrastructure.Persistence.CrossCutting;

/// <summary>
/// A notification record — one row per dispatched (or attempted) message across any
/// channel. Email/SMS go out via the configured gateway (logged in dev); InApp is
/// record-only. Cross-cutting Infrastructure entity (like AuditLogEntry / DocumentAttachment).
/// </summary>
public class Notification : BaseTransactionalEntity
{
    public string Channel { get; set; } = string.Empty;      // Email | Sms | InApp
    public string Recipient { get; set; } = string.Empty;    // email / phone / role / userId
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    public string? RelatedEntityType { get; set; }
    public long? RelatedEntityId { get; set; }

    public string Status { get; set; } = "Pending";          // Pending | Sent | Failed
    public string? Error { get; set; }
    public DateTimeOffset? SentAt { get; set; }
}
