namespace BengalTex.ERP.Application.Common.Interfaces;

/// <summary>
/// Abstract file storage. Backed by local disk in development; can be swapped
/// for S3/Azure Blob in production by registering a different IFileStorage in DI.
///
/// Paths returned from SaveAsync are storage-implementation-relative
/// (e.g., "SalesOrder/2026-05/abc.pdf" for local disk) and stored verbatim
/// on DocumentAttachment.StoragePath. The storage implementation knows how
/// to resolve them on OpenReadAsync / DeleteAsync.
/// </summary>
public interface IFileStorage
{
    Task<FileStorageResult> SaveAsync(
        Stream content,
        string originalFileName,
        string contentType,
        string entityType,
        CancellationToken ct = default);

    Task<Stream> OpenReadAsync(string storagePath, CancellationToken ct = default);

    Task DeleteAsync(string storagePath, CancellationToken ct = default);

    Task<bool> ExistsAsync(string storagePath, CancellationToken ct = default);
}

public record FileStorageResult(
    string StoredFileName,    // GUID + original extension
    string StoragePath,       // relative path stored in DocumentAttachment
    long SizeBytes);
