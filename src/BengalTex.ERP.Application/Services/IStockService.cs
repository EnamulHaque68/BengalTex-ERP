using BengalTex.ERP.Domain.Entities;

namespace BengalTex.ERP.Application.Services;

public interface IStockService
{
    /// <summary>
    /// Posts a single RawMaterial stock movement and upserts the corresponding
    /// <c>StockOnHand</c> row. Does NOT call <c>SaveChanges</c> — the caller's commit
    /// ties this together with its source-document update so the whole thing is atomic.
    /// <paramref name="lot"/> (optional, kept last to avoid disturbing existing callers)
    /// tags the movement with a <c>StockLot</c> for batch traceability — pass a NEW (unsaved,
    /// caller-added) or existing tracked lot; the FK resolves via navigation on commit.
    /// </summary>
    Task PostRawMaterialMovementAsync(
        int rawMaterialId,
        int warehouseId,
        decimal signedQuantity,
        StockMovementType movementType,
        string? referenceType,
        long? referenceId,
        string? referenceCode,
        DateOnly movementDate,
        string? notes,
        CancellationToken ct = default,
        StockLot? lot = null);

    /// <summary>
    /// Posts a single Product stock movement (production receipt, sales dispatch, etc.)
    /// and upserts the corresponding <c>StockOnHand</c> row. Same atomicity contract as
    /// <see cref="PostRawMaterialMovementAsync"/>. <paramref name="lot"/> optionally tags
    /// the movement with a <c>StockLot</c> for batch traceability.
    /// </summary>
    Task PostProductMovementAsync(
        int productId,
        int warehouseId,
        decimal signedQuantity,
        StockMovementType movementType,
        string? referenceType,
        long? referenceId,
        string? referenceCode,
        DateOnly movementDate,
        string? notes,
        CancellationToken ct = default,
        StockLot? lot = null);

    /// <summary>
    /// Current StockOnHand quantity for the given (RawMaterial × Warehouse). Returns 0
    /// if no row exists yet. Use this for pre-flight stock-availability checks (e.g.
    /// Production Complete validation).
    /// </summary>
    Task<decimal> GetRawMaterialOnHandAsync(
        int rawMaterialId,
        int warehouseId,
        CancellationToken ct = default);

    /// <summary>
    /// Current StockOnHand quantity for the given (Product × Warehouse). Returns 0 if
    /// no row exists yet. Use this for pre-flight stock-availability checks (e.g.
    /// Delivery Note Post validation).
    /// </summary>
    Task<decimal> GetProductOnHandAsync(
        int productId,
        int warehouseId,
        CancellationToken ct = default);

    /// <summary>
    /// Total StockOnHand quantity for a RawMaterial summed across ALL warehouses.
    /// Used as the denominator for global weighted-average-cost recomputation on GRN Post.
    /// </summary>
    Task<decimal> GetRawMaterialTotalOnHandAsync(
        int rawMaterialId,
        CancellationToken ct = default);

    /// <summary>
    /// Total StockOnHand quantity for a Product summed across ALL warehouses.
    /// Used as the denominator for global weighted-average-cost recomputation on
    /// Production Complete.
    /// </summary>
    Task<decimal> GetProductTotalOnHandAsync(
        int productId,
        CancellationToken ct = default);
}
