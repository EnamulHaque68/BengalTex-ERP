namespace BengalTex.ERP.Application.Services;

/// <summary>
/// Soft inventory reservation (Phase 2). Earmarks stock against a source document without
/// moving it — only bumps <c>StockOnHand.ReservedQuantity</c> (Available = Quantity − Reserved).
/// All methods follow the same atomicity contract as <see cref="IStockService"/>: they do NOT
/// call <c>SaveChanges</c>, so the caller ties the reservation change into one transaction with
/// its document update.
/// </summary>
public interface IStockReservationService
{
    /// <summary>
    /// Reserves a production order's BOM raw materials + semi-finished component products in its
    /// issue warehouse (single-PO BOM explosion: qty × (1 + wastage%) × order/BOM-output scale).
    /// Adds <c>StockReservation</c>(Active) rows + increments the matching <c>StockOnHand.ReservedQuantity</c>.
    /// No-op when the BOM has no lines / zero output. Does NOT SaveChanges.
    /// </summary>
    Task ReserveForProductionOrderAsync(long productionOrderId, CancellationToken ct = default);

    /// <summary>
    /// Releases every Active reservation for a source document (marks them Released + decrements
    /// <c>StockOnHand.ReservedQuantity</c>). Idempotent — no Active rows → no-op. Does NOT SaveChanges.
    /// </summary>
    Task ReleaseForReferenceAsync(string referenceType, long referenceId, CancellationToken ct = default);

    /// <summary>Current reserved quantity for a (RawMaterial × Warehouse). 0 when no snapshot row exists.</summary>
    Task<decimal> GetReservedRawMaterialAsync(int rawMaterialId, int warehouseId, CancellationToken ct = default);

    /// <summary>Current reserved quantity for a (Product × Warehouse). 0 when no snapshot row exists.</summary>
    Task<decimal> GetReservedProductAsync(int productId, int warehouseId, CancellationToken ct = default);
}
