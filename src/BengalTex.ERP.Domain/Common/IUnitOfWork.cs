namespace BengalTex.ERP.Domain.Common;

public interface IUnitOfWork
{
    /// <summary>
    /// Saves changes and dispatches domain events after successful commit.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    Task<IDisposable> BeginTransactionAsync(CancellationToken ct = default);
}