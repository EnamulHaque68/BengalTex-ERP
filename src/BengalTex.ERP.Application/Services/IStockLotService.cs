using BengalTex.ERP.Domain.Entities;

namespace BengalTex.ERP.Application.Services;

/// <summary>
/// Lot-aware outbound stock consumption. Draws a quantity down from the available
/// <see cref="StockLot"/>s of a RawMaterial in a warehouse, oldest-first (FIFO), decrementing
/// each lot's <c>CurrentQuantity</c> (marking it Depleted at zero) and posting one
/// lot-tagged <see cref="StockMovement"/> per lot slice via <see cref="IStockService"/>.
///
/// Any quantity beyond what the lots cover (stock received before lot tracking, or via GRN
/// lines without a lot number) is posted as a single un-tagged movement — so the behaviour is
/// identical to a plain <c>PostRawMaterialMovementAsync</c> when no lots exist. Does NOT
/// SaveChanges — the calling command owns the commit (atomic with its document update).
/// </summary>
public interface IStockLotService
{
    Task ConsumeRawMaterialFifoAsync(
        int rawMaterialId,
        int warehouseId,
        decimal quantity,                 // positive amount to consume; the service posts it as outbound
        StockMovementType movementType,
        string? referenceType,
        long? referenceId,
        string? referenceCode,
        DateOnly movementDate,
        string? notes,
        CancellationToken ct = default);
}
