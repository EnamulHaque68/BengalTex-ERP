using BengalTex.ERP.Application.Attachments.Dtos;

namespace BengalTex.ERP.Application.Services;

/// <summary>
/// Manages polymorphic document attachments. Lives behind this interface (impl in
/// Infrastructure) because <c>DocumentAttachment</c> is an Infrastructure cross-cutting
/// entity — same inversion pattern as <see cref="IStockService"/> / IAuditLogQueryService.
/// Combines DB metadata (DocumentAttachments table) with byte storage (IFileStorage).
/// </summary>
public interface IAttachmentService
{
    Task<AttachmentDto> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        string entityType,
        long entityId,
        string? description,
        string? category,
        CancellationToken ct = default);

    Task<IReadOnlyList<AttachmentDto>> GetForEntityAsync(
        string entityType, long entityId, CancellationToken ct = default);

    /// <summary>Opens the file stream + metadata for download, or null if not found.</summary>
    Task<AttachmentDownload?> OpenAsync(long id, CancellationToken ct = default);

    /// <summary>Deletes both the DB row and the stored file. Returns false if not found.</summary>
    Task<bool> DeleteAsync(long id, CancellationToken ct = default);
}

/// <summary>File payload for a download response.</summary>
public sealed record AttachmentDownload(Stream Content, string FileName, string ContentType);
