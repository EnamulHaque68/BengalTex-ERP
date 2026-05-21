using BengalTex.ERP.Application.Attachments.Dtos;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Infrastructure.Persistence;
using BengalTex.ERP.Infrastructure.Persistence.CrossCutting;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Infrastructure.Services;

/// <summary>
/// Combines DB metadata (DocumentAttachments table) with byte storage (<see cref="IFileStorage"/>).
/// Implements <see cref="IAttachmentService"/> for the Application layer (which can't reference
/// the Infrastructure-layer <c>DocumentAttachment</c> entity directly).
/// </summary>
public sealed class AttachmentService : IAttachmentService
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorage _storage;

    public AttachmentService(ApplicationDbContext db, IFileStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<AttachmentDto> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        string entityType,
        long entityId,
        string? description,
        string? category,
        CancellationToken ct = default)
    {
        var stored = await _storage.SaveAsync(content, fileName, contentType, entityType, ct);

        var entity = new DocumentAttachment
        {
            EntityType = entityType,
            EntityId = entityId,
            FileName = fileName,
            StoredFileName = stored.StoredFileName,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            FileSizeBytes = stored.SizeBytes,
            StoragePath = stored.StoragePath,
            Description = description,
            Category = category,
        };

        _db.DocumentAttachments.Add(entity);
        await _db.SaveChangesAsync(ct);   // CreatedAt/CreatedBy stamped by AuditInterceptor

        return Map(entity);
    }

    public async Task<IReadOnlyList<AttachmentDto>> GetForEntityAsync(
        string entityType, long entityId, CancellationToken ct = default)
    {
        return await _db.DocumentAttachments.AsNoTracking()
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AttachmentDto(
                a.Id, a.EntityType, a.EntityId, a.FileName, a.ContentType,
                a.FileSizeBytes, a.Description, a.Category, a.CreatedAt, a.CreatedBy))
            .ToListAsync(ct);
    }

    public async Task<AttachmentDownload?> OpenAsync(long id, CancellationToken ct = default)
    {
        var entity = await _db.DocumentAttachments.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, ct);
        if (entity is null) return null;
        if (!await _storage.ExistsAsync(entity.StoragePath, ct)) return null;

        var stream = await _storage.OpenReadAsync(entity.StoragePath, ct);
        return new AttachmentDownload(stream, entity.FileName, entity.ContentType);
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken ct = default)
    {
        var entity = await _db.DocumentAttachments.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (entity is null) return false;

        // Remove the physical file first; the DB row is soft-deleted by the AuditInterceptor.
        await _storage.DeleteAsync(entity.StoragePath, ct);
        _db.DocumentAttachments.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static AttachmentDto Map(DocumentAttachment a) => new(
        a.Id, a.EntityType, a.EntityId, a.FileName, a.ContentType,
        a.FileSizeBytes, a.Description, a.Category, a.CreatedAt, a.CreatedBy);
}
