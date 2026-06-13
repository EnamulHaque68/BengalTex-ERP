using BengalTex.ERP.Application.Reports.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Reports.Queries;

/// <summary>
/// Per-product gross-margin report over customer-invoice lines in a date window.
/// Revenue = Σ (line.Quantity × line.UnitPrice) — net of VAT (lines are always net).
/// COGS = Σ (line.Quantity × cost-at-sale), where cost-at-sale = the line's
/// <c>UnitCostAtSale</c> snapshot taken at Issue time, falling back to the Product's
/// CURRENT weighted-average cost for legacy lines issued before snapshotting existed.
/// Includes only invoices with Status ∈ {Issued, PartiallyPaid, Paid} (non-Draft, non-Cancelled).
///
/// Default window if both dates omitted: trailing 30 days ending today.
/// </summary>
public sealed record GetMarginReportQuery(
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    int? CustomerId = null
) : IRequest<ApiResponse<MarginReportDto>>;

internal sealed class GetMarginReportQueryHandler
    : IRequestHandler<GetMarginReportQuery, ApiResponse<MarginReportDto>>
{
    private readonly IRepository<Domain.Entities.CustomerInvoice, long> _invRepo;
    private readonly IRepository<Domain.Entities.Customer> _customerRepo;

    public GetMarginReportQueryHandler(
        IRepository<Domain.Entities.CustomerInvoice, long> invRepo,
        IRepository<Domain.Entities.Customer> customerRepo)
    {
        _invRepo = invRepo;
        _customerRepo = customerRepo;
    }

    public async Task<ApiResponse<MarginReportDto>> Handle(
        GetMarginReportQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var to = request.ToDate ?? today;
        var from = request.FromDate ?? to.AddDays(-30);

        var invBase = _invRepo.Query()
            .Where(i => i.InvoiceDate >= from && i.InvoiceDate <= to
                     && i.Status != Domain.Entities.CustomerInvoiceStatus.Draft
                     && i.Status != Domain.Entities.CustomerInvoiceStatus.Cancelled);
        if (request.CustomerId.HasValue)
            invBase = invBase.Where(i => i.CustomerId == request.CustomerId.Value);

        // Flatten to invoice lines (JOIN). Cost-at-sale = the snapshot taken at Issue;
        // legacy rows (null) fall back to the product's current WAC.
        var lineData = await invBase
            .SelectMany(i => i.Lines, (i, l) => new
            {
                l.ProductId,
                ProductCode = l.Product.Code,
                ProductName = l.Product.Name,
                UomCode = l.Product.UnitOfMeasure.Code,
                l.Quantity,
                l.UnitPrice,
                i.ExchangeRate,                          // revenue → BDT (cost is already BDT)
                UnitCost = l.UnitCostAtSale ?? l.Product.WeightedAverageCost
            })
            .ToListAsync(cancellationToken);

        var rows = lineData
            .GroupBy(x => new { x.ProductId, x.ProductCode, x.ProductName, x.UomCode })
            .Select(g =>
            {
                var qty = g.Sum(x => x.Quantity);
                var revenue = g.Sum(x => x.Quantity * x.UnitPrice * x.ExchangeRate);   // → BDT
                var cogs = g.Sum(x => x.Quantity * x.UnitCost);                        // cost already BDT
                var margin = revenue - cogs;
                // Weighted-average unit cost across the window's lines (display only).
                var avgUnitCost = qty != 0m ? cogs / qty : 0m;
                return new MarginReportRowDto(
                    g.Key.ProductId,
                    g.Key.ProductCode,
                    g.Key.ProductName,
                    g.Key.UomCode,
                    qty,
                    revenue,
                    cogs,
                    avgUnitCost,
                    margin,
                    revenue != 0m ? Math.Round(margin / revenue * 100m, 2, MidpointRounding.AwayFromZero) : 0m);
            })
            .OrderByDescending(r => r.Margin)
            .ToList();

        var totalRevenue = rows.Sum(r => r.Revenue);
        var totalCogs = rows.Sum(r => r.Cogs);
        var totalMargin = totalRevenue - totalCogs;

        string? customerName = null;
        if (request.CustomerId.HasValue)
        {
            var c = await _customerRepo.GetByIdAsync(request.CustomerId.Value, cancellationToken);
            customerName = c?.Name;
        }

        var report = new MarginReportDto(
            FromDate: from,
            ToDate: to,
            CustomerId: request.CustomerId,
            CustomerName: customerName,
            ProductCount: rows.Count,
            TotalRevenue: totalRevenue,
            TotalCogs: totalCogs,
            TotalMargin: totalMargin,
            OverallMarginPercent: totalRevenue != 0m
                ? Math.Round(totalMargin / totalRevenue * 100m, 2, MidpointRounding.AwayFromZero)
                : 0m,
            Rows: rows);

        return ApiResponse<MarginReportDto>.Ok(report);
    }
}
