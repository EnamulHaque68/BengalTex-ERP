using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Infrastructure.Services;

/// <summary>
/// Soft inventory reservation (Phase 2). Keeps a <see cref="StockReservation"/> ledger and the
/// denormalized <see cref="StockOnHand.ReservedQuantity"/> snapshot in sync. Reservation never
/// touches <see cref="StockOnHand.Quantity"/> — it only earmarks. Caller owns SaveChanges so the
/// reservation change is atomic with the source document (PO create/complete/cancel).
/// </summary>
public sealed class StockReservationService : IStockReservationService
{
    private readonly IRepository<StockReservation, long> _reservationRepo;
    private readonly IRepository<StockOnHand> _onHandRepo;
    private readonly IRepository<ProductionOrder, long> _poRepo;

    public StockReservationService(
        IRepository<StockReservation, long> reservationRepo,
        IRepository<StockOnHand> onHandRepo,
        IRepository<ProductionOrder, long> poRepo)
    {
        _reservationRepo = reservationRepo;
        _onHandRepo = onHandRepo;
        _poRepo = poRepo;
    }

    public async Task ReserveForProductionOrderAsync(long productionOrderId, CancellationToken ct = default)
    {
        var po = await _poRepo.Query()
            .Include(p => p.Bom).ThenInclude(b => b.Lines)
            .FirstOrDefaultAsync(p => p.Id == productionOrderId, ct);

        if (po?.Bom is null || po.Bom.Lines.Count == 0 || po.Bom.OutputQuantity <= 0m)
            return;

        var scale = po.Quantity / po.Bom.OutputQuantity;

        // Aggregate per distinct item first — a BOM that lists the same RM twice must yield ONE
        // reservation row (and ONE StockOnHand touch) so we never create a duplicate snapshot row.
        var rmRequired = new Dictionary<int, decimal>();
        var componentRequired = new Dictionary<int, decimal>();
        foreach (var line in po.Bom.Lines)
        {
            var qty = line.Quantity * (1 + line.WastagePercent / 100m) * scale;
            if (qty <= 0m) continue;

            if (line.RawMaterialId is int rmId)
                rmRequired[rmId] = rmRequired.GetValueOrDefault(rmId) + qty;
            else if (line.ComponentProductId is int cpId)
                componentRequired[cpId] = componentRequired.GetValueOrDefault(cpId) + qty;
        }

        foreach (var (rmId, qty) in rmRequired)
            await ReserveInternalAsync(rmId, null, po.IssueWarehouseId, qty, "ProductionOrder", po.Id, po.Code, ct);
        foreach (var (cpId, qty) in componentRequired)
            await ReserveInternalAsync(null, cpId, po.IssueWarehouseId, qty, "ProductionOrder", po.Id, po.Code, ct);
    }

    public async Task ReleaseForReferenceAsync(string referenceType, long referenceId, CancellationToken ct = default)
    {
        var active = await _reservationRepo.Query()
            .Where(r => r.ReferenceType == referenceType
                && r.ReferenceId == referenceId
                && r.Status == ReservationStatus.Active)
            .ToListAsync(ct);

        if (active.Count == 0) return;

        foreach (var res in active)
        {
            res.Status = ReservationStatus.Released;
            res.ReleasedAt = DateTimeOffset.UtcNow;
            _reservationRepo.Update(res);

            var onHand = res.RawMaterialId.HasValue
                ? await _onHandRepo.Query().FirstOrDefaultAsync(s => s.RawMaterialId == res.RawMaterialId && s.WarehouseId == res.WarehouseId, ct)
                : await _onHandRepo.Query().FirstOrDefaultAsync(s => s.ProductId == res.ProductId && s.WarehouseId == res.WarehouseId, ct);

            if (onHand is not null)
            {
                onHand.ReservedQuantity -= res.Quantity;
                if (onHand.ReservedQuantity < 0m) onHand.ReservedQuantity = 0m;   // safety floor
                _onHandRepo.Update(onHand);
            }
        }
    }

    public async Task<decimal> GetReservedRawMaterialAsync(int rawMaterialId, int warehouseId, CancellationToken ct = default)
    {
        var row = await _onHandRepo.Query()
            .FirstOrDefaultAsync(s => s.RawMaterialId == rawMaterialId && s.WarehouseId == warehouseId, ct);
        return row?.ReservedQuantity ?? 0m;
    }

    public async Task<decimal> GetReservedProductAsync(int productId, int warehouseId, CancellationToken ct = default)
    {
        var row = await _onHandRepo.Query()
            .FirstOrDefaultAsync(s => s.ProductId == productId && s.WarehouseId == warehouseId, ct);
        return row?.ReservedQuantity ?? 0m;
    }

    private async Task ReserveInternalAsync(
        int? rawMaterialId, int? productId, int warehouseId, decimal qty,
        string referenceType, long referenceId, string? referenceCode, CancellationToken ct)
    {
        await _reservationRepo.AddAsync(new StockReservation
        {
            RawMaterialId = rawMaterialId,
            ProductId = productId,
            WarehouseId = warehouseId,
            Quantity = qty,
            Status = ReservationStatus.Active,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            ReferenceCode = referenceCode,
            ReservedAt = DateTimeOffset.UtcNow
        }, ct);

        // Upsert the StockOnHand snapshot's ReservedQuantity (the row may not exist yet — soft reserve).
        var onHand = rawMaterialId.HasValue
            ? await _onHandRepo.Query().FirstOrDefaultAsync(s => s.RawMaterialId == rawMaterialId && s.WarehouseId == warehouseId, ct)
            : await _onHandRepo.Query().FirstOrDefaultAsync(s => s.ProductId == productId && s.WarehouseId == warehouseId, ct);

        if (onHand is null)
        {
            await _onHandRepo.AddAsync(new StockOnHand
            {
                RawMaterialId = rawMaterialId,
                ProductId = productId,
                WarehouseId = warehouseId,
                Quantity = 0m,
                ReservedQuantity = qty
            }, ct);
        }
        else
        {
            onHand.ReservedQuantity += qty;
            _onHandRepo.Update(onHand);
        }
    }
}
