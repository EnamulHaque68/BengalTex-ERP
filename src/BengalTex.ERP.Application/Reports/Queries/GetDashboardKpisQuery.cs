using BengalTex.ERP.Application.Reports.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Reports.Queries;

/// <summary>
/// Top-of-dashboard at-a-glance numbers — counts + monetary totals across stock, AR,
/// AP, and current-month sales. Four parallel aggregations, each a single COUNT/SUM
/// over filtered indexes. Cheap to compute even on a busy day.
/// </summary>
public sealed record GetDashboardKpisQuery() : IRequest<ApiResponse<DashboardKpisDto>>;

internal sealed class GetDashboardKpisQueryHandler
    : IRequestHandler<GetDashboardKpisQuery, ApiResponse<DashboardKpisDto>>
{
    private readonly IRepository<Domain.Entities.StockOnHand> _stockRepo;
    private readonly IRepository<Domain.Entities.CustomerInvoice, long> _arRepo;
    private readonly IRepository<Domain.Entities.SupplierInvoice, long> _apRepo;

    public GetDashboardKpisQueryHandler(
        IRepository<Domain.Entities.StockOnHand> stockRepo,
        IRepository<Domain.Entities.CustomerInvoice, long> arRepo,
        IRepository<Domain.Entities.SupplierInvoice, long> apRepo)
    {
        _stockRepo = stockRepo;
        _arRepo = arRepo;
        _apRepo = apRepo;
    }

    public async Task<ApiResponse<DashboardKpisDto>> Handle(
        GetDashboardKpisQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        // Distinct item count = distinct (RawMaterialId or ProductId) rows with qty > 0
        var stockItemCount = await _stockRepo.Query()
            .Where(s => s.Quantity > 0m)
            .Select(s => s.RawMaterialId != null
                ? "R:" + s.RawMaterialId.Value
                : "P:" + s.ProductId!.Value)
            .Distinct()
            .CountAsync(cancellationToken);

        var arOutstandingAgg = await _arRepo.Query()
            .Where(i => (i.Status == Domain.Entities.CustomerInvoiceStatus.Issued
                      || i.Status == Domain.Entities.CustomerInvoiceStatus.PartiallyPaid)
                     && i.TotalAmount - i.AmountPaid > 0m)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                Sum = g.Sum(x => x.TotalAmount - x.AmountPaid)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var apOutstandingAgg = await _apRepo.Query()
            .Where(i => (i.Status == Domain.Entities.SupplierInvoiceStatus.Approved
                      || i.Status == Domain.Entities.SupplierInvoiceStatus.PartiallyPaid)
                     && i.TotalAmount - i.AmountPaid > 0m)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                Sum = g.Sum(x => x.TotalAmount - x.AmountPaid)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var thisMonthSalesAgg = await _arRepo.Query()
            .Where(i => i.InvoiceDate >= monthStart && i.InvoiceDate <= monthEnd
                     && i.Status != Domain.Entities.CustomerInvoiceStatus.Draft
                     && i.Status != Domain.Entities.CustomerInvoiceStatus.Cancelled)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                Sum = g.Sum(x => x.TotalAmount)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var dto = new DashboardKpisDto(
            GeneratedAt: DateTimeOffset.UtcNow,
            StockItemCount: stockItemCount,
            OutstandingArAmount: arOutstandingAgg?.Sum ?? 0m,
            OutstandingArInvoiceCount: arOutstandingAgg?.Count ?? 0,
            OutstandingApAmount: apOutstandingAgg?.Sum ?? 0m,
            OutstandingApInvoiceCount: apOutstandingAgg?.Count ?? 0,
            ThisMonthSalesAmount: thisMonthSalesAgg?.Sum ?? 0m,
            ThisMonthSalesInvoiceCount: thisMonthSalesAgg?.Count ?? 0,
            MonthStart: monthStart,
            MonthEnd: monthEnd);

        return ApiResponse<DashboardKpisDto>.Ok(dto);
    }
}
