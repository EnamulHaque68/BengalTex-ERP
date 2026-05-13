using BengalTex.ERP.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace BengalTex.ERP.Infrastructure.Services;

/// <summary>
/// Local-disk file storage. Layout: {RootPath}/{EntityType}/{YYYY-MM}/{guid}{ext}
/// Examples (with RootPath = "uploads"):
///   uploads/SalesOrder/2026-05/a4b5c6d7....pdf
///   uploads/Compliance/2026-05/9e8f7d6b....png
/// </summary>
public class LocalFileStorage : IFileStorage
{
    private readonly string _rootPath;

    public LocalFileStorage(IOptions<FileStorageSettings> settings)
    {
        var configured = settings.Value.RootPath;
        _rootPath = Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(Directory.GetCurrentDirectory(), configured);

        Directory.CreateDirectory(_rootPath);
    }

    public async Task<FileStorageResult> SaveAsync(
        Stream content,
        string originalFileName,
        string contentType,
        string entityType,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(entityType))
            throw new ArgumentException("EntityType is required for partitioning the storage layout.", nameof(entityType));

        var ext = Path.GetExtension(originalFileName);
        var storedFileName = $"{Guid.NewGuid():N}{ext}";
        var yearMonth = DateTimeOffset.UtcNow.ToString("yyyy-MM");

        // Forward-slash relative path for portability (stored in DocumentAttachment.StoragePath)
        var relativePath = $"{entityType}/{yearMonth}/{storedFileName}";
        var absoluteDir = Path.Combine(_rootPath, entityType, yearMonth);
        Directory.CreateDirectory(absoluteDir);

        var absolutePath = Path.Combine(absoluteDir, storedFileName);

        long size;
        await using (var output = File.Create(absolutePath))
        {
            await content.CopyToAsync(output, ct);
            size = output.Length;
        }

        return new FileStorageResult(storedFileName, relativePath, size);
    }

    public Task<Stream> OpenReadAsync(string storagePath, CancellationToken ct = default)
    {
        var absolutePath = ResolveAndGuard(storagePath);
        if (!File.Exists(absolutePath))
            throw new FileNotFoundException("File not found in storage.", storagePath);

        Stream stream = File.OpenRead(absolutePath);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storagePath, CancellationToken ct = default)
    {
        var absolutePath = ResolveAndGuard(storagePath);
        if (File.Exists(absolutePath))
            File.Delete(absolutePath);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string storagePath, CancellationToken ct = default) =>
        Task.FromResult(File.Exists(ResolveAndGuard(storagePath)));

    private string ResolveAndGuard(string storagePath)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
            throw new ArgumentException("Storage path is required.", nameof(storagePath));

        var normalized = storagePath.Replace('/', Path.DirectorySeparatorChar);
        var combined = Path.GetFullPath(Path.Combine(_rootPath, normalized));
        var rootFull = Path.GetFullPath(_rootPath);

        // Block path-traversal: combined path must remain under _rootPath
        if (!combined.StartsWith(rootFull + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) &&
            !combined.Equals(rootFull, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException($"Path traversal blocked: '{storagePath}'.");
        }

        return combined;
    }
}
