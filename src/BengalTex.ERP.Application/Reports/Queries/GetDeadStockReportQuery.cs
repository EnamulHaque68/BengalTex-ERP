using BengalTex.ERP.Application.Reports.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Reports.Queries;

/// <summary>
/// Dead-stock report — items with on-hand quantity whose most recent stock movement is at least
/// <paramref name="DaysThreshold"/> days old (or which have never moved). Optional item-type and
/// warehouse scoping; ordered by tied-up value (largest first).
/// </summary>
public sealed record GetDeadStockReportQuery(
    int DaysThreshold = 90,
    string? ItemType = null,           // "RawMaterial" | "Product" | null = both
    int? WarehouseId = null,
    DateOnly? AsOfDate = null
) : IRequest<ApiResponse<DeadStockReportDto>>;

internal sealed class GetDeadStockReportQueryHandler
    : IRequestHandler<GetDeadStockReportQuery, ApiResponse<DeadStockReportDto>>
{
    private readonly IRepository<Domain.Entities.StockOnHand> _stockRepo;
    private readonly IRepository<Domain.Entities.StockMovement, long> _movementRepo;
    private readonly IRepository<Domain.Entities.Warehouse> _warehouseRepo;

    public GetDeadStockReportQueryHandler(
        IRepository<Domain.Entities.StockOnHand> stockRepo,
        IRepository<Domain.Entities.StockMovement, long> movementRepo,
        IRepository<Domain.Entities.Warehouse> warehouseRepo)
    {
        _stockRepo = stockRepo;
        _movementRepo = movementRepo;
        _warehouseRepo = warehouseRepo;
    }

    public async Task<ApiResponse<DeadStockReportDto>> Handle(
        GetDeadStockReportQuery req, CancellationToken ct)
    {
        var days = req.DaysThreshold <= 0 ? 90 : req.DaysThreshold;
        var asOf = req.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var cutoff = asOf.AddDays(-days);   // last movement on/before this date = dead

        var includeRm = !string.Equals(req.ItemType, "Product", StringComparison.OrdinalIgnoreCase);
        var includeProduct = !string.Equals(req.ItemType, "RawMaterial", StringComparison.OrdinalIgnoreCase);

        string? warehouseName = null;
        if (req.WarehouseId.HasValue)
        {
            var wh = await _warehouseRepo.GetByIdAsync(req.WarehouseId.Value, ct);
            if (wh is null) return ApiResponse<DeadStockReportDto>.Fail("Warehouse not found.");
            warehouseName = wh.Name;
        }

        var rows = new List<DeadStockRowDto>();

        if (includeRm)
            rows.AddRange(await BuildAsync(isRm: true, req.WarehouseId, cutoff, asOf, ct));
        if (includeProduct)
            rows.AddRange(await BuildAsync(isRm: false, req.WarehouseId, cutoff, asOf, ct));

        var ordered = rows.OrderByDescending(r => r.Value).ThenBy(r => r.Name).ToList();

        var dto = new DeadStockReportDto(
            GeneratedAt: DateTimeOffset.UtcNow,
            AsOfDate: asOf,
            DaysThreshold: days,
            WarehouseId: req.WarehouseId,
            WarehouseName: warehouseName,
            ItemType: req.ItemType,
            RowCount: ordered.Count,
            TotalDeadValue: ordered.Sum(r => r.Value),
            Rows: ordered);

        return ApiResponse<DeadStockReportDto>.Ok(dto);
    }

    private async Task<List<DeadStockRowDto>> BuildAsync(
        bool isRm, int? warehouseId, DateOnly cutoff, DateOnly asOf, CancellationToken ct)
    {
        // 1) Current on-hand per item (qty != 0), optionally in one warehouse.
        var stockQuery = _stockRepo.Query().Where(s =>
            (isRm ? s.RawMaterialId != null : s.ProductId != null) && s.Quantity != 0m);
        if (warehouseId.HasValue)
            stockQuery = stockQuery.Where(s => s.WarehouseId == warehouseId.Value);

        var stockRows = isRm
            ? await stockQuery.GroupBy(s => new
                {
                    Id = s.RawMaterialId!.Value,
                    s.RawMaterial!.Code,
                    s.RawMaterial!.Name,
                    UomCode = s.RawMaterial!.UnitOfMeasure.Code,
                    UnitCost = s.RawMaterial!.WeightedAverageCost
                })
                .Select(g => new ItemStock(g.Key.Id, g.Key.Code, g.Key.Name, g.Key.UomCode, g.Key.UnitCost, g.Sum(x => x.Quantity)))
                .ToListAsync(ct)
            : await stockQuery.GroupBy(s => new
                {
                    Id = s.ProductId!.Value,
                    s.Product!.Code,
                    s.Product!.Name,
                    UomCode = s.Product!.UnitOfMeasure.Code,
                    UnitCost = s.Product!.WeightedAverageCost
                })
                .Select(g => new ItemStock(g.Key.Id, g.Key.Code, g.Key.Name, g.Key.UomCode, g.Key.UnitCost, g.Sum(x => x.Quantity)))
                .ToListAsync(ct);

        if (stockRows.Count == 0) return new List<DeadStockRowDto>();

        // 2) Last movement date per item (same warehouse scope).
        var moveQuery = _movementRepo.Query().Where(m => isRm ? m.RawMaterialId != null : m.ProductId != null);
        if (warehouseId.HasValue)
            moveQuery = moveQuery.Where(m => m.WarehouseId == warehouseId.Value);

        var lastMoves = await moveQuery
            .GroupBy(m => isRm ? m.RawMaterialId!.Value : m.ProductId!.Value)
            .Select(g => new { ItemId = g.Key, Last = g.Max(x => x.MovementDate) })
            .ToListAsync(ct);
        var lastByItem = lastMoves.ToDictionary(x => x.ItemId, x => x.Last);

        // 3) Keep only items whose last movement is on/before the cutoff (or never moved).
        var result = new List<DeadStockRowDto>();
        foreach (var s in stockRows)
        {
            DateOnly? last = lastByItem.TryGetValue(s.Id, out var d) ? d : null;
            if (last.HasValue && last.Value > cutoff) continue;   // moved recently → not dead

            var daysSince = last.HasValue ? asOf.DayNumber - last.Value.DayNumber : 9999;
            result.Add(new DeadStockRowDto(
                isRm ? "RawMaterial" : "Product",
                isRm ? s.Id : null,
                isRm ? null : s.Id,
                s.Code, s.Name, s.UomCode,
                s.Qty, s.UnitCost, s.Qty * s.UnitCost,
                last, daysSince));
        }
        return result;
    }

    private sealed record ItemStock(int Id, string Code, string Name, string UomCode, decimal UnitCost, decimal Qty);
}
