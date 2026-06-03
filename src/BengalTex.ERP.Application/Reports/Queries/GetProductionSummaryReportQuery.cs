using BengalTex.ERP.Application.Reports.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Reports.Queries;

/// <summary>
/// Per-product rollup of production output for orders whose ActualEndDate (or CompletedAt
/// fallback) falls inside the supplied date window. Each row aggregates order count, total
/// FG quantity produced, average qty per order, and average cycle time in days
/// (ActualEndDate − ActualStartDate). Default window if both dates omitted: trailing 30 days.
/// </summary>
public sealed record GetProductionSummaryReportQuery(
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    int? ProductId = null
) : IRequest<ApiResponse<ProductionSummaryReportDto>>;

internal sealed class GetProductionSummaryReportQueryHandler
    : IRequestHandler<GetProductionSummaryReportQuery, ApiResponse<ProductionSummaryReportDto>>
{
    private readonly IRepository<ProductionOrder, long> _repo;
    public GetProductionSummaryReportQueryHandler(IRepository<ProductionOrder, long> repo) => _repo = repo;

    public async Task<ApiResponse<ProductionSummaryReportDto>> Handle(
        GetProductionSummaryReportQuery q, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var toDate = q.ToDate ?? today;
        var fromDate = q.FromDate ?? toDate.AddDays(-30);

        var query = _repo.Query()
            .Where(p => p.Status == ProductionOrderStatus.Completed
                     && p.ActualEndDate != null
                     && p.ActualEndDate >= fromDate
                     && p.ActualEndDate <= toDate);
        if (q.ProductId.HasValue) query = query.Where(p => p.ProductId == q.ProductId.Value);

        // Materialise first (cycle-time math is awkward in pure EF translation)
        var orders = await query
            .Select(p => new
            {
                p.ProductId, ProductCode = p.Product.Code, ProductName = p.Product.Name,
                p.Quantity, p.ActualStartDate, p.ActualEndDate
            })
            .ToListAsync(ct);

        var rows = orders
            .GroupBy(o => new { o.ProductId, o.ProductCode, o.ProductName })
            .Select(g =>
            {
                var orderCount = g.Count();
                var totalQty = g.Sum(o => o.Quantity);
                var avgQty = orderCount > 0 ? Math.Round(totalQty / orderCount, 4, MidpointRounding.AwayFromZero) : 0m;
                var withDates = g.Where(o => o.ActualStartDate.HasValue && o.ActualEndDate.HasValue).ToList();
                var avgCycleDays = withDates.Count == 0 ? 0m
                    : Math.Round((decimal)withDates.Average(o =>
                        o.ActualEndDate!.Value.DayNumber - o.ActualStartDate!.Value.DayNumber), 1);
                return new ProductionSummaryRowDto(
                    g.Key.ProductId, g.Key.ProductCode, g.Key.ProductName,
                    orderCount, totalQty, avgQty, avgCycleDays);
            })
            .OrderByDescending(r => r.TotalQuantityProduced)
            .ToList();

        return ApiResponse<ProductionSummaryReportDto>.Ok(new ProductionSummaryReportDto(
            fromDate, toDate,
            rows.Sum(r => r.OrderCount),
            rows.Sum(r => r.TotalQuantityProduced),
            rows));
    }
}
