using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Infrastructure.Services;

/// <summary>
/// Posts stock movements and keeps the StockOnHand snapshot in sync. Movement codes
/// auto-generate from the "MV" numbering series. Caller is responsible for the
/// SaveChanges call — this lets a single business operation (e.g. GRN Post, Stock
/// Adjustment Post) tie the document update and the movement(s) into one transaction.
/// </summary>
public sealed class StockService : IStockService
{
    private readonly IRepository<StockMovement, long> _movementRepo;
    private readonly IRepository<StockOnHand> _onHandRepo;
    private readonly INumberingService _numbering;

    public StockService(
        IRepository<StockMovement, long> movementRepo,
        IRepository<StockOnHand> onHandRepo,
        INumberingService numbering)
    {
        _movementRepo = movementRepo;
        _onHandRepo = onHandRepo;
        _numbering = numbering;
    }

    public async Task PostMovementAsync(
        int rawMaterialId,
        int warehouseId,
        decimal signedQuantity,
        StockMovementType movementType,
        string? referenceType,
        long? referenceId,
        string? referenceCode,
        DateOnly movementDate,
        string? notes,
        CancellationToken ct = default)
    {
        if (rawMaterialId <= 0)
            throw new ArgumentException("Raw material id is required.", nameof(rawMaterialId));
        if (warehouseId <= 0)
            throw new ArgumentException("Warehouse id is required.", nameof(warehouseId));
        if (signedQuantity == 0m)
            throw new InvalidOperationException("Cannot post a zero-quantity stock movement.");

        var code = await _numbering.NextAsync("MV", null, ct);

        var movement = new StockMovement
        {
            Code = code,
            RawMaterialId = rawMaterialId,
            WarehouseId = warehouseId,
            SignedQuantity = signedQuantity,
            MovementType = movementType,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            ReferenceCode = referenceCode,
            MovementDate = movementDate,
            Notes = notes
        };
        await _movementRepo.AddAsync(movement, ct);

        // Upsert the StockOnHand snapshot for this (RM × Warehouse)
        var onHand = await _onHandRepo.Query()
            .FirstOrDefaultAsync(s => s.RawMaterialId == rawMaterialId && s.WarehouseId == warehouseId, ct);
        if (onHand is null)
        {
            onHand = new StockOnHand
            {
                RawMaterialId = rawMaterialId,
                WarehouseId = warehouseId,
                Quantity = signedQuantity
            };
            await _onHandRepo.AddAsync(onHand, ct);
        }
        else
        {
            onHand.Quantity += signedQuantity;
            _onHandRepo.Update(onHand);
        }
    }
}
