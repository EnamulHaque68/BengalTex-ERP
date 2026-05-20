using BengalTex.ERP.Application.AuditLog.Dtos;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Infrastructure.Services;

/// <summary>
/// Read-only queries over the AuditLogEntries table. Lives in Infrastructure because
/// <c>AuditLogEntry</c> is an Infrastructure-layer cross-cutting entity; exposed to the
/// Application layer through <see cref="IAuditLogQueryService"/>.
/// </summary>
public sealed class AuditLogQueryService : IAuditLogQueryService
{
    private readonly ApplicationDbContext _db;

    public AuditLogQueryService(ApplicationDbContext db) => _db = db;

    public async Task<PagedResult<AuditLogEntryDto>> QueryAsync(
        PagedQueryParameters parameters,
        string? entityType = null,
        string? action = null,
        string? userName = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken ct = default)
    {
        var query = _db.AuditLogEntries.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(a => a.EntityType == entityType);
        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action);
        if (!string.IsNullOrWhiteSpace(userName))
            query = query.Where(a => a.UserName != null && a.UserName.Contains(userName));

        if (fromDate.HasValue)
        {
            var fromTs = new DateTimeOffset(fromDate.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            query = query.Where(a => a.Timestamp >= fromTs);
        }
        if (toDate.HasValue)
        {
            var toTs = new DateTimeOffset(toDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            query = query.Where(a => a.Timestamp < toTs);
        }

        var search = parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(a =>
                a.EntityType.Contains(search) ||
                a.EntityKey.Contains(search) ||
                (a.UserName != null && a.UserName.Contains(search)));
        }

        // Newest first — audit review is almost always "what happened recently"
        query = query.OrderByDescending(a => a.Timestamp);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(a => new AuditLogEntryDto(
                a.Id, a.EntityType, a.EntityKey, a.Action,
                a.UserName, a.IpAddress, a.AffectedColumns,
                a.OldValuesJson, a.NewValuesJson, a.Timestamp))
            .ToListAsync(ct);

        return PagedResult<AuditLogEntryDto>.Create(items, parameters.Page, parameters.PageSize, totalCount);
    }

    public async Task<IReadOnlyList<string>> GetEntityTypesAsync(CancellationToken ct = default)
    {
        return await _db.AuditLogEntries.AsNoTracking()
            .Select(a => a.EntityType)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync(ct);
    }
}
