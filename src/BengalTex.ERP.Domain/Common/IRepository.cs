using System.Linq.Expressions;

namespace BengalTex.ERP.Domain.Common;

/// <summary>
/// Generic repository interface. Use sparingly — prefer specific repositories
/// that expose only the queries needed by their bounded context.
/// </summary>
public interface IRepository<TEntity, TKey>
    where TEntity : BaseEntity<TKey>
    where TKey : struct
{
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default);
    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
    IQueryable<TEntity> Query();
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default);
    Task AddAsync(TEntity entity, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);
    void Update(TEntity entity);
    void Remove(TEntity entity); // Soft-deletes via interceptor
}

public interface IRepository<TEntity> : IRepository<TEntity, int>
    where TEntity : BaseEntity<int>
{ }