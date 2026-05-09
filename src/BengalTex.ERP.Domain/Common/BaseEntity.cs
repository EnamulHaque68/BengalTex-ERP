namespace BengalTex.ERP.Domain.Common;

/// <summary>
/// Base for all entities except ASP.NET Identity entities (which have Guid keys).
/// Provides audit fields and soft-delete.
/// </summary>
public abstract class BaseEntity<TKey> : IAuditable, ISoftDeletable, IHasDomainEvents
    where TKey : struct
{
    public TKey Id { get; set; }

    // Audit fields
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // Optimistic concurrency
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // Domain events (not persisted)
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void RaiseDomainEvent(IDomainEvent @event) => _domainEvents.Add(@event);
    public void ClearDomainEvents() => _domainEvents.Clear();
}

/// <summary>
/// Default int-keyed base for master entities.
/// </summary>
public abstract class BaseEntity : BaseEntity<int> { }

/// <summary>
/// Long-keyed base for high-volume transactional entities (StockLedger, AuditLog, Attendance).
/// </summary>
public abstract class BaseTransactionalEntity : BaseEntity<long> { }