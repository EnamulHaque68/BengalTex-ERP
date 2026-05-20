using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Infrastructure.Services;

/// <summary>
/// Posts stock movements (RawMaterial or Product) and keeps the StockOnHand snapshot
/// in sync. Movement codes auto-generate from the "MV" numbering series. Caller is
/// responsible for the SaveChanges call — this lets a single business operation
/// (GRN Post, Adjustment Post, Production Complete) tie the document update and the
/// movement(s) into one transaction.
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

    public Task PostRawMaterialMovementAsync(
        int rawMaterialId, int warehouseId, decimal signedQuantity,
        StockMovementType movementType, string? referenceType, long? referenceId,
        string? referenceCode, DateOnly movementDate, string? notes,
        CancellationToken ct = default)
    {
        if (rawMaterialId <= 0)
            throw new ArgumentException("Raw material id is required.", nameof(rawMaterialId));

        return PostInternalAsync(
            rawMaterialId: rawMaterialId, productId: null,
            warehouseId, signedQuantity, movementType,
            referenceType, referenceId, referenceCode, movementDate, notes, ct);
    }

    public Task PostProductMovementAsync(
        int productId, int warehouseId, decimal signedQuantity,
        StockMovementType movementType, string? referenceType, long? referenceId,
        string? referenceCode, DateOnly movementDate, string? notes,
        CancellationToken ct = default)
    {
        if (productId <= 0)
            throw new ArgumentException("Product id is required.", nameof(productId));

        return PostInternalAsync(
            rawMaterialId: null, productId: productId,
            warehouseId, signedQuantity, movementType,
            referenceType, referenceId, referenceCode, movementDate, notes, ct);
    }

    public async Task<decimal> GetRawMaterialOnHandAsync(
        int rawMaterialId, int warehouseId, CancellationToken ct = default)
    {
        var row = await _onHandRepo.Query()
            .FirstOrDefaultAsync(s => s.RawMaterialId == rawMaterialId && s.WarehouseId == warehouseId, ct);
        return row?.Quantity ?? 0m;
    }

    public async Task<decimal> GetProductOnHandAsync(
        int productId, int warehouseId, CancellationToken ct = default)
    {
        var row = await _onHandRepo.Query()
            .FirstOrDefaultAsync(s => s.ProductId == productId && s.WarehouseId == warehouseId, ct);
        return row?.Quantity ?? 0m;
    }

    public async Task<decimal> GetRawMaterialTotalOnHandAsync(
        int rawMaterialId, CancellationToken ct = default)
    {
        return await _onHandRepo.Query()
            .Where(s => s.RawMaterialId == rawMaterialId)
            .SumAsync(s => (decimal?)s.Quantity, ct) ?? 0m;
    }

    public async Task<decimal> GetProductTotalOnHandAsync(
        int productId, CancellationToken ct = default)
    {
        return await _onHandRepo.Query()
            .Where(s => s.ProductId == productId)
            .SumAsync(s => (decimal?)s.Quantity, ct) ?? 0m;
    }

    private async Task PostInternalAsync(
        int? rawMaterialId, int? productId, int warehouseId, decimal signedQuantity,
        StockMovementType movementType, string? referenceType, long? referenceId,
        string? referenceCode, DateOnly movementDate, string? notes, CancellationToken ct)
    {
        if (warehouseId <= 0)
            throw new ArgumentException("Warehouse id is required.", nameof(warehouseId));
        if (signedQuantity == 0m)
            throw new InvalidOperationException("Cannot post a zero-quantity stock movement.");

        var code = await _numbering.NextAsync("MV", null, ct);

        var movement = new StockMovement
        {
            Code = code,
            RawMaterialId = rawMaterialId,
            ProductId = productId,
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

        // Upsert the StockOnHand snapshot for this (item × warehouse)
        var onHand = rawMaterialId.HasValue
            ? await _onHandRepo.Query()
                .FirstOrDefaultAsync(s => s.RawMaterialId == rawMaterialId && s.WarehouseId == warehouseId, ct)
            : await _onHandRepo.Query()
                .FirstOrDefaultAsync(s => s.ProductId == productId && s.WarehouseId == warehouseId, ct);

        if (onHand is null)
        {
            onHand = new StockOnHand
            {
                RawMaterialId = rawMaterialId,
                ProductId = productId,
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
