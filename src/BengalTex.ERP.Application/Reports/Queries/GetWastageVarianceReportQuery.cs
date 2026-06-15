using BengalTex.ERP.Application.Reports.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Reports.Queries;

/// <summary>
/// Standard-vs-actual wastage variance for completed production orders in a date range (optionally
/// one product). Standard wastage cost comes from each order's BOM allowance; actual from the
/// WastageEntry rows linked to the order. Ordered by variance (worst overspend first).
/// </summary>
public sealed record GetWastageVarianceReportQuery(
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    int? ProductId = null
) : IRequest<ApiResponse<WastageVarianceReportDto>>;

internal sealed class GetWastageVarianceReportQueryHandler
    : IRequestHandler<GetWastageVarianceReportQuery, ApiResponse<WastageVarianceReportDto>>
{
    private readonly IRepository<ProductionOrder, long> _poRepo;
    private readonly IRepository<WastageEntry, long> _wastageRepo;

    public GetWastageVarianceReportQueryHandler(
        IRepository<ProductionOrder, long> poRepo,
        IRepository<WastageEntry, long> wastageRepo)
    {
        _poRepo = poRepo;
        _wastageRepo = wastageRepo;
    }

    public async Task<ApiResponse<WastageVarianceReportDto>> Handle(
        GetWastageVarianceReportQuery req, CancellationToken ct)
    {
        // Completed orders in range (by actual end date) with their BOM lines + RM costs.
        var query = _poRepo.Query().Where(p => p.Status == ProductionOrderStatus.Completed);
        if (req.FromDate.HasValue) query = query.Where(p => p.ActualEndDate >= req.FromDate.Value);
        if (req.ToDate.HasValue) query = query.Where(p => p.ActualEndDate <= req.ToDate.Value);
        if (req.ProductId.HasValue) query = query.Where(p => p.ProductId == req.ProductId.Value);

        var orders = await query
            .Select(p => new
            {
                p.Id,
                p.Code,
                p.ActualEndDate,
                p.ProductId,
                ProductCode = p.Product.Code,
                ProductName = p.Product.Name,
                p.Quantity,
                BomOutput = p.Bom.OutputQuantity,
                Lines = p.Bom.Lines.Select(l => new
                {
                    l.Quantity,
                    l.WastagePercent,
                    Wac = l.RawMaterial.WeightedAverageCost
                }).ToList()
            })
            .ToListAsync(ct);

        if (orders.Count == 0)
        {
            return ApiResponse<WastageVarianceReportDto>.Ok(new WastageVarianceReportDto(
                DateTimeOffset.UtcNow, req.FromDate, req.ToDate, req.ProductId, 0, 0m, 0m, 0m,
                new List<WastageVarianceRowDto>()));
        }

        // Actual wastage cost per production order (linked WastageEntry rows).
        var orderIds = orders.Select(o => o.Id).ToList();
        var actualByOrder = (await _wastageRepo.Query()
                .Where(w => w.ProductionOrderId != null && orderIds.Contains(w.ProductionOrderId!.Value))
                .GroupBy(w => w.ProductionOrderId!.Value)
                .Select(g => new { OrderId = g.Key, Actual = g.Sum(x => x.TotalCost) })
                .ToListAsync(ct))
            .ToDictionary(x => x.OrderId, x => x.Actual);

        var rows = new List<WastageVarianceRowDto>(orders.Count);
        foreach (var o in orders)
        {
            var scale = o.BomOutput > 0m ? o.Quantity / o.BomOutput : 0m;
            // BOM-built-in wastage allowance cost = Σ (line qty × wastage% × scale) × RM WAC.
            var standard = o.Lines.Sum(l => l.Quantity * (l.WastagePercent / 100m) * scale * l.Wac);
            var actual = actualByOrder.TryGetValue(o.Id, out var a) ? a : 0m;
            var variance = actual - standard;
            var variancePct = standard > 0m ? Math.Round(variance / standard * 100m, 1) : 0m;

            rows.Add(new WastageVarianceRowDto(
                o.Id, o.Code, o.ActualEndDate, o.ProductId, o.ProductCode, o.ProductName,
                o.Quantity,
                Math.Round(standard, 2), Math.Round(actual, 2), Math.Round(variance, 2), variancePct));
        }

        var ordered = rows.OrderByDescending(r => r.Variance).ToList();
        var dto = new WastageVarianceReportDto(
            GeneratedAt: DateTimeOffset.UtcNow,
            FromDate: req.FromDate,
            ToDate: req.ToDate,
            ProductId: req.ProductId,
            RowCount: ordered.Count,
            TotalStandardCost: ordered.Sum(r => r.StandardWastageCost),
            TotalActualCost: ordered.Sum(r => r.ActualWastageCost),
            TotalVariance: ordered.Sum(r => r.Variance),
            Rows: ordered);

        return ApiResponse<WastageVarianceReportDto>.Ok(dto);
    }
}
