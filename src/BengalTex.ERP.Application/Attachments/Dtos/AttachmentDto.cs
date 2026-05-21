namespace BengalTex.ERP.Application.Attachments.Dtos;

/// <summary>
/// A document attachment's metadata (the file bytes are streamed separately via download).
/// </summary>
public sealed record AttachmentDto(
    long Id,
    string EntityType,
    long EntityId,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string? Description,
    string? Category,
    DateTimeOffset UploadedAt,
    string? UploadedBy);
