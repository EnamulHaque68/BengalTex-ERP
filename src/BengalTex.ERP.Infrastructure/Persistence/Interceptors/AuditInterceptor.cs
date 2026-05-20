using System.Text.Json;
using System.Text.Json.Serialization;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Infrastructure.Persistence.CrossCutting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BengalTex.ERP.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Two responsibilities on every save:
/// 1. Stamps audit fields (CreatedBy/ModifiedBy/…) and converts hard deletes to soft
///    deletes for <see cref="ISoftDeletable"/> entities.
/// 2. Writes a granular <see cref="AuditLogEntry"/> change-history row per affected
///    top-level document (master + transactional entities). Child line-item rows
///    (<c>*Line</c>) and high-churn infra tables are excluded — see <see cref="_excludedTypes"/>.
///
/// Old/new values are captured during <c>SavingChanges</c> (originals are still available),
/// the DB-generated key is read during <c>SavedChanges</c>, then the audit rows are persisted
/// with a second save. The second save tracks only excluded <c>AuditLogEntry</c> rows, so it
/// produces no further audits and the cycle terminates.
/// </summary>
public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _dateTime;

    // Entity type names that are too high-churn or self-referential to audit.
    private static readonly HashSet<string> _excludedTypes = new(StringComparer.Ordinal)
    {
        nameof(AuditLogEntry),    // recursion guard — the audit rows themselves
        "StockOnHand",            // snapshot — rewritten on every posting
        "StockMovement",          // already an immutable stock ledger
        "NumberingSeries",        // counter increments on every document creation
        "OutboxMessage",          // infra messaging plumbing
    };

    // Audit-metadata columns: never interesting in a business change diff.
    private static readonly HashSet<string> _ignoredProps = new(StringComparer.Ordinal)
    {
        nameof(IAuditable.CreatedAt), nameof(IAuditable.CreatedBy),
        nameof(IAuditable.ModifiedAt), nameof(IAuditable.ModifiedBy),
        "RowVersion",
    };

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
    };

    // Captured during SavingChanges, drained during SavedChanges.
    private readonly List<PendingAudit> _pending = new();

    public AuditInterceptor(ICurrentUserService currentUser, IDateTimeProvider dateTime)
    {
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    // ── SavingChanges: stamp audit fields + snapshot old/new values ───────────

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            ApplyAuditing(eventData.Context);
            CaptureAudits(eventData.Context);
        }
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
        {
            ApplyAuditing(eventData.Context);
            CaptureAudits(eventData.Context);
        }
        return base.SavingChanges(eventData, result);
    }

    // ── SavedChanges: resolve generated keys + persist the audit rows ─────────

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null && _pending.Count > 0)
        {
            var rows = DrainAndBuild();
            eventData.Context.Set<AuditLogEntry>().AddRange(rows);
            await eventData.Context.SaveChangesAsync(cancellationToken);
        }
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (eventData.Context is not null && _pending.Count > 0)
        {
            var rows = DrainAndBuild();
            eventData.Context.Set<AuditLogEntry>().AddRange(rows);
            eventData.Context.SaveChanges();
        }
        return base.SavedChanges(eventData, result);
    }

    // ── Audit-field stamping + soft-delete conversion ─────────────────────────

    private void ApplyAuditing(DbContext context)
    {
        var now = _dateTime.UtcNow;
        var user = _currentUser.UserId ?? "system";

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is not IAuditable auditable) continue;

            switch (entry.State)
            {
                case EntityState.Added:
                    auditable.CreatedAt = now;
                    auditable.CreatedBy = user;
                    break;

                case EntityState.Modified:
                    auditable.ModifiedAt = now;
                    auditable.ModifiedBy = user;
                    // Preserve original created fields
                    entry.Property(nameof(IAuditable.CreatedAt)).IsModified = false;
                    entry.Property(nameof(IAuditable.CreatedBy)).IsModified = false;
                    break;

                case EntityState.Deleted when entry.Entity is ISoftDeletable softDeletable:
                    entry.State = EntityState.Modified;
                    softDeletable.IsDeleted = true;
                    softDeletable.DeletedAt = now;
                    softDeletable.DeletedBy = user;
                    break;
            }
        }
    }

    // ── Change-history capture (documents only) ───────────────────────────────

    private void CaptureAudits(DbContext context)
    {
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is not IAuditable) continue;

            var typeName = entry.Metadata.ClrType.Name;
            if (_excludedTypes.Contains(typeName)) continue;
            if (typeName.EndsWith("Line", StringComparison.Ordinal)) continue;   // child line items

            string action;
            string? oldJson = null, newJson = null, affected = null;

            switch (entry.State)
            {
                case EntityState.Added:
                    action = "Insert";
                    newJson = Serialize(ValuesOf(entry, useOriginal: false));
                    break;

                case EntityState.Modified when IsSoftDelete(entry):
                    action = "Delete";                       // soft-delete already converted by ApplyAuditing
                    oldJson = Serialize(ValuesOf(entry, useOriginal: true));
                    break;

                case EntityState.Modified:
                    var changed = entry.Properties
                        .Where(p => p.IsModified && !_ignoredProps.Contains(p.Metadata.Name))
                        .ToList();
                    if (changed.Count == 0) continue;        // metadata-only touch — nothing to record
                    action = "Update";
                    oldJson = Serialize(changed.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue));
                    newJson = Serialize(changed.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue));
                    affected = string.Join(", ", changed.Select(p => p.Metadata.Name));
                    break;

                case EntityState.Deleted:                    // true hard delete (rare — every entity is soft-deletable)
                    action = "Delete";
                    oldJson = Serialize(ValuesOf(entry, useOriginal: true));
                    break;

                default:
                    continue;
            }

            _pending.Add(new PendingAudit(entry, typeName, action, oldJson, newJson, affected));
        }
    }

    private List<AuditLogEntry> DrainAndBuild()
    {
        var batch = _pending.ToList();
        _pending.Clear();

        var now = _dateTime.UtcNow;
        var userId = _currentUser.UserId;
        var userName = _currentUser.UserName;
        var ip = _currentUser.IpAddress;
        var ua = Truncate(_currentUser.UserAgent, 500);

        return batch.Select(p => new AuditLogEntry
        {
            EntityType = p.EntityType,
            EntityKey = PrimaryKey(p.Entry),
            Action = p.Action,
            UserId = userId,
            UserName = userName,
            IpAddress = ip,
            UserAgent = ua,
            OldValuesJson = p.OldValuesJson,
            NewValuesJson = p.NewValuesJson,
            AffectedColumns = Truncate(p.AffectedColumns, 2000),
            Timestamp = now,
        }).ToList();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool IsSoftDelete(EntityEntry entry)
    {
        var prop = entry.Properties.FirstOrDefault(p => p.Metadata.Name == nameof(ISoftDeletable.IsDeleted));
        return prop is not null && prop.IsModified && prop.CurrentValue is true;
    }

    private static Dictionary<string, object?> ValuesOf(EntityEntry entry, bool useOriginal) =>
        entry.Properties
            .Where(p => !_ignoredProps.Contains(p.Metadata.Name))
            .ToDictionary(p => p.Metadata.Name, p => useOriginal ? p.OriginalValue : p.CurrentValue);

    private static string? Serialize(IReadOnlyDictionary<string, object?> values) =>
        values.Count == 0 ? null : JsonSerializer.Serialize(values, _jsonOptions);

    private static string PrimaryKey(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key is null) return string.Empty;
        return string.Join("|", key.Properties.Select(p => entry.Property(p.Name).CurrentValue?.ToString() ?? ""));
    }

    private static string? Truncate(string? value, int max) =>
        value is { Length: > 0 } && value.Length > max ? value[..max] : value;

    private sealed record PendingAudit(
        EntityEntry Entry,
        string EntityType,
        string Action,
        string? OldValuesJson,
        string? NewValuesJson,
        string? AffectedColumns);
}
