using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BengalTex.ERP.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Stamps audit fields on insert/update/delete. Converts hard deletes to soft deletes
/// for entities implementing ISoftDeletable.
/// </summary>
public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _dateTime;

    public AuditInterceptor(ICurrentUserService currentUser, IDateTimeProvider dateTime)
    {
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null) ApplyAuditing(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null) ApplyAuditing(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

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
}