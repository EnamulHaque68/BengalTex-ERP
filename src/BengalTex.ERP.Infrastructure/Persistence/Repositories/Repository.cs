using System.Linq.Expressions;
using BengalTex.ERP.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Infrastructure.Persistence.Repositories;

public class Repository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : BaseEntity<TKey>
    where TKey : struct
{
    protected readonly ApplicationDbContext Db;
    protected readonly DbSet<TEntity> Set;

    public Repository(ApplicationDbContext db)
    {
        Db = db;
        Set = db.Set<TEntity>();
    }

    public Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(e => e.Id.Equals(id), ct);

    public Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(predicate, ct);

    public IQueryable<TEntity> Query() => Set.AsQueryable();

    public Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default) =>
        Set.AnyAsync(predicate, ct);

    public Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default) =>
        predicate is null ? Set.CountAsync(ct) : Set.CountAsync(predicate, ct);

    public async Task AddAsync(TEntity entity, CancellationToken ct = default) =>
        await Set.AddAsync(entity, ct);

    public Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default) =>
        Set.AddRangeAsync(entities, ct);

    public void Update(TEntity entity) => Set.Update(entity);
    public void Remove(TEntity entity) => Set.Remove(entity); // becomes soft-delete via interceptor
}

public class Repository<TEntity> : Repository<TEntity, int>, IRepository<TEntity>
    where TEntity : BaseEntity<int>
{
    public Repository(ApplicationDbContext db) : base(db) { }
}