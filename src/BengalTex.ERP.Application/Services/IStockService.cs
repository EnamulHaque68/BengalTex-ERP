using BengalTex.ERP.Domain.Entities;

namespace BengalTex.ERP.Application.Services;

public interface IStockService
{
    /// <summary>
    /// Posts a single stock movement and upserts the corresponding StockOnHand row.
    /// Does NOT call SaveChanges — the caller's commit ties this together with its
    /// source-document update so the whole thing is one atomic transaction.
    /// </summary>
    Task PostMovementAsync(
        int rawMaterialId,
        int warehouseId,
        decimal signedQuantity,
        StockMovementType movementType,
        string? referenceType,
        long? referenceId,
        string? referenceCode,
        DateOnly movementDate,
        string? notes,
        CancellationToken ct = default);
}
