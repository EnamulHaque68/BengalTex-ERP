using BengalTex.ERP.Application.Mrp.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Mrp.Queries;

/// <summary>
/// Computes the Material Requirement Planning run (Phase 3). Read-only aggregation over the
/// reservation snapshot (demand) + on-hand + open purchase orders (supply). No schema impact.
/// </summary>
public sealed record GetMrpQuery(bool ShortageOnly = false) : IRequest<ApiResponse<MrpResultDto>>;

internal sealed class GetMrpQueryHandler
    : IRequestHandler<GetMrpQuery, ApiResponse<MrpResultDto>>
{
    private readonly IRepository<StockOnHand> _onHand;
    private readonly IRepository<PurchaseOrderLine, long> _poLines;
    private readonly IRepository<Domain.Entities.RawMaterial> _rmRepo;

    public GetMrpQueryHandler(
        IRepository<StockOnHand> onHand,
        IRepository<PurchaseOrderLine, long> poLines,
        IRepository<Domain.Entities.RawMaterial> rmRepo)
    {
        _onHand = onHand;
        _poLines = poLines;
        _rmRepo = rmRepo;
    }

    public async Task<ApiResponse<MrpResultDto>> Handle(GetMrpQuery request, CancellationToken ct)
    {
        // ── Demand + on-hand: aggregate the StockOnHand snapshot per raw material across warehouses
        // (materialize-then-group in memory to dodge nested-aggregate translation issues). ──
        var stockRows = await _onHand.Query()
            .Where(s => s.RawMaterialId != null)
            .Select(s => new { RmId = s.RawMaterialId!.Value, s.Quantity, s.ReservedQuantity })
            .ToListAsync(ct);

        var stockByRm = stockRows
            .GroupBy(x => x.RmId)
            .ToDictionary(
                g => g.Key,
                g => (OnHand: g.Sum(x => x.Quantity), Reserved: g.Sum(x => x.ReservedQuantity)));

        // ── Incoming supply: open Purchase Orders (committed but not fully received). ──
        var openStatuses = new[]
        {
            PurchaseOrderStatus.Approved,
            PurchaseOrderStatus.Sent,
            PurchaseOrderStatus.PartiallyReceived
        };
        var openPoLines = await _poLines.Query()
            .Where(l => openStatuses.Contains(l.PurchaseOrder.Status))
            .Select(l => new { l.RawMaterialId, Remaining = l.Quantity - l.ReceivedQuantity })
            .ToListAsync(ct);

        var incomingByRm = openPoLines
            .GroupBy(x => x.RawMaterialId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Remaining > 0m ? x.Remaining : 0m));

        // ── Demand set: raw materials with firm production demand (Reserved > 0). ──
        var demandRmIds = stockByRm.Where(kv => kv.Value.Reserved > 0m).Select(kv => kv.Key).ToList();
        if (demandRmIds.Count == 0)
            return ApiResponse<MrpResultDto>.Ok(new MrpResultDto(Array.Empty<MrpItemDto>(), 0, 0m));

        var rms = await _rmRepo.Query()
            .Where(r => demandRmIds.Contains(r.Id))
            .Select(r => new
            {
                r.Id,
                r.Code,
                r.Name,
                Uom = r.UnitOfMeasure.Code,
                r.WeightedAverageCost,
                r.MinimumStockLevel
            })
            .ToListAsync(ct);

        var items = new List<MrpItemDto>(rms.Count);
        foreach (var rm in rms)
        {
            var (onHand, reserved) = stockByRm.TryGetValue(rm.Id, out var s) ? s : (0m, 0m);
            var incoming = incomingByRm.TryGetValue(rm.Id, out var inc) ? inc : 0m;
            var available = onHand - reserved;
            var shortage = reserved - onHand - incoming;
            if (shortage < 0m) shortage = 0m;

            if (request.ShortageOnly && shortage <= 0m) continue;

            items.Add(new MrpItemDto(
                rm.Id, rm.Code, rm.Name, rm.Uom,
                reserved, onHand, available, incoming, shortage,
                rm.WeightedAverageCost, rm.MinimumStockLevel));
        }

        items = items
            .OrderByDescending(i => i.ShortageQuantity)
            .ThenBy(i => i.RawMaterialName)
            .ToList();

        var result = new MrpResultDto(
            items,
            items.Count(i => i.ShortageQuantity > 0m),
            items.Sum(i => i.ShortageQuantity * i.EstimatedUnitPrice));

        return ApiResponse<MrpResultDto>.Ok(result);
    }
}
