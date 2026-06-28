using BengalTex.ERP.Application.Reports.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Reports.Queries;

/// <summary>
/// Actual production cost sheet per completed production order in the date window (Phase 6).
/// Cost = Material (auto) + Labour + Machine + Overhead + Subcontract + Wastage + Reject. Where the
/// order is sales-linked, revenue (SO line price × qty, in BDT) and gross profit are included.
/// Default window = trailing 30 days; filters by ActualEndDate.
/// </summary>
public sealed record GetProductionCostReportQuery(
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    int? ProductId = null
) : IRequest<ApiResponse<ProductionCostReportDto>>;

internal sealed class GetProductionCostReportQueryHandler
    : IRequestHandler<GetProductionCostReportQuery, ApiResponse<ProductionCostReportDto>>
{
    private readonly IRepository<ProductionOrder, long> _repo;
    public GetProductionCostReportQueryHandler(IRepository<ProductionOrder, long> repo) => _repo = repo;

    public async Task<ApiResponse<ProductionCostReportDto>> Handle(
        GetProductionCostReportQuery req, CancellationToken ct)
    {
        var toDate = req.ToDate ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var fromDate = req.FromDate ?? toDate.AddDays(-30);

        var query = _repo.Query()
            .AsNoTracking()
            .Where(p => p.Status == ProductionOrderStatus.Completed
                     && p.ActualEndDate != null
                     && p.ActualEndDate >= fromDate && p.ActualEndDate <= toDate);

        if (req.ProductId.HasValue)
            query = query.Where(p => p.ProductId == req.ProductId.Value);

        var prods = await query
            .OrderByDescending(p => p.ActualEndDate)
            .Include(p => p.Product)
            .Include(p => p.Bom).ThenInclude(b => b.Lines).ThenInclude(l => l.RawMaterial)
            .Include(p => p.Bom).ThenInclude(b => b.Lines).ThenInclude(l => l.ComponentProduct)
            .Include(p => p.SalesOrder)
            .Include(p => p.SalesOrderLine)
            .ToListAsync(ct);

        var items = prods.Select(p =>
        {
            // Material cost: captured-at-Complete value, or fall back to live BOM × weighted-average-cost.
            var scale = p.Bom.OutputQuantity > 0m ? p.Quantity / p.Bom.OutputQuantity : 0m;
            var computedMaterial = Math.Round(p.Bom.Lines.Sum(l =>
            {
                var q = l.Quantity * (1 + l.WastagePercent / 100m) * scale;
                var wac = l.RawMaterialId != null
                    ? (l.RawMaterial?.WeightedAverageCost ?? 0m)
                    : (l.ComponentProduct?.WeightedAverageCost ?? 0m);
                return q * wac;
            }), 2);
            var material = p.MaterialCost > 0m ? p.MaterialCost : computedMaterial;

            var total = material + p.LabourCost + p.MachineCost + p.OverheadCost
                      + p.SubcontractCost + p.WastageCost + p.RejectCost;
            var costPerUnit = p.Quantity > 0m ? Math.Round(total / p.Quantity, 4) : 0m;

            decimal? revenue = p.SalesOrderLine != null
                ? Math.Round(p.SalesOrderLine.UnitPrice * p.Quantity * (p.SalesOrder?.ExchangeRate ?? 1m), 2)
                : null;
            decimal? grossProfit = revenue.HasValue ? revenue.Value - total : null;
            decimal? grossMargin = revenue is > 0m ? Math.Round(grossProfit!.Value / revenue.Value * 100m, 2) : null;

            return new ProductionCostRowDto(
                p.Id, p.Code, p.Product.Code, p.Product.Name, p.Quantity, p.ActualEndDate,
                p.SalesOrder != null ? p.SalesOrder.Code : null,
                material, p.LabourCost, p.MachineCost, p.OverheadCost,
                p.SubcontractCost, p.WastageCost, p.RejectCost,
                total, costPerUnit, revenue, grossProfit, grossMargin);
        }).ToList();

        var result = new ProductionCostReportDto(
            fromDate, toDate,
            items.Count,
            items.Sum(i => i.TotalCost),
            items.Sum(i => i.Revenue ?? 0m),
            items.Sum(i => i.GrossProfit ?? 0m),
            items);

        return ApiResponse<ProductionCostReportDto>.Ok(result);
    }
}
