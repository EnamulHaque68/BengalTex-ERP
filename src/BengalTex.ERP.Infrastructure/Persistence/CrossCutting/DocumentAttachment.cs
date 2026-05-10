using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Infrastructure.Persistence.CrossCutting;

/// <summary>
/// Polymorphic attachment. (EntityType, EntityId) points to any entity in the system.
/// </summary>
public class DocumentAttachment : BaseTransactionalEntity
{
    public string EntityType { get; set; } = string.Empty;   // e.g., "SalesOrder", "Customer"
    public long EntityId { get; set; }
    public string FileName { get; set; } = string.Empty;     // Original
    public string StoredFileName { get; set; } = string.Empty; // GUID-based
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }                    // "Invoice", "Compliance", etc.
}