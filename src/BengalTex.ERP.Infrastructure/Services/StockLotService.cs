using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Infrastructure.Services;

/// <summary>
/// Implements FIFO lot consumption over <see cref="StockLot"/>. Posts the actual stock
/// movements through <see cref="IStockService"/> (which also keeps StockOnHand in sync),
/// so the StockOnHand total nets to the same value whether or not lots are involved.
/// </summary>
public sealed class StockLotService : IStockLotService
{
    private readonly IRepository<StockLot, long> _lotRepo;
    private readonly IStockService _stock;

    public StockLotService(IRepository<StockLot, long> lotRepo, IStockService stock)
    {
        _lotRepo = lotRepo;
        _stock = stock;
    }

    public async Task ConsumeRawMaterialFifoAsync(
        int rawMaterialId, int warehouseId, decimal quantity,
        StockMovementType movementType, string? referenceType, long? referenceId,
        string? referenceCode, DateOnly movementDate, string? notes, CancellationToken ct = default)
    {
        if (quantity <= 0m) return;

        var remaining = quantity;

        // Oldest active lots first (FIFO) — tracked so CurrentQuantity changes persist on the caller's commit.
        var lots = await _lotRepo.Query()
            .Where(l => l.RawMaterialId == rawMaterialId
                     && l.WarehouseId == warehouseId
                     && l.CurrentQuantity > 0
                     && l.Status == LotStatus.Active)
            .OrderBy(l => l.ReceivedDate).ThenBy(l => l.Id)
            .ToListAsync(ct);

        foreach (var lot in lots)
        {
            if (remaining <= 0m) break;

            var take = Math.Min(remaining, lot.CurrentQuantity);
            lot.CurrentQuantity -= take;
            if (lot.CurrentQuantity <= 0m) lot.Status = LotStatus.Depleted;
            _lotRepo.Update(lot);

            await _stock.PostRawMaterialMovementAsync(
                rawMaterialId, warehouseId, -take, movementType,
                referenceType, referenceId, referenceCode, movementDate, notes, ct, lot);

            remaining -= take;
        }

        // Quantity the lots couldn't cover — post un-tagged (pre-lot-era or lot-less stock).
        if (remaining > 0m)
        {
            await _stock.PostRawMaterialMovementAsync(
                rawMaterialId, warehouseId, -remaining, movementType,
                referenceType, referenceId, referenceCode, movementDate, notes, ct, null);
        }
    }
}
