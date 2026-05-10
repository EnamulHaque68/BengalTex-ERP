using BengalTex.ERP.Domain.Common;
using Microsoft.EntityFrameworkCore.Storage;

namespace BengalTex.ERP.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _db;
    public UnitOfWork(ApplicationDbContext db) => _db = db;

    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);

    public async Task<IDisposable> BeginTransactionAsync(CancellationToken ct = default)
    {
        var tx = await _db.Database.BeginTransactionAsync(ct);
        return new TransactionScope(tx);
    }

    private sealed class TransactionScope : IDisposable
    {
        private readonly IDbContextTransaction _tx;
        public TransactionScope(IDbContextTransaction tx) => _tx = tx;
        public void Dispose() => _tx.Dispose();
    }
}