using BengalTex.ERP.Application.Reports.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Reports.Queries;

/// <summary>
/// Per-customer rollup of Sales activity in a date window — Sales Orders (by OrderDate),
/// Delivery Notes (by DispatchDate, Posted only), and Customer Invoices (by InvoiceDate,
/// non-Draft, non-Cancelled). Three separate per-customer aggregations merged in-memory.
/// Default window if both dates omitted: trailing 30 days ending today.
/// </summary>
public sealed record GetSalesSummaryReportQuery(
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    int? CustomerId = null
) : IRequest<ApiResponse<SalesSummaryReportDto>>;

internal sealed class GetSalesSummaryReportQueryHandler
    : IRequestHandler<GetSalesSummaryReportQuery, ApiResponse<SalesSummaryReportDto>>
{
    private readonly IRepository<Domain.Entities.SalesOrder, long> _soRepo;
    private readonly IRepository<Domain.Entities.DeliveryNote, long> _dnRepo;
    private readonly IRepository<Domain.Entities.CustomerInvoice, long> _invRepo;
    private readonly IRepository<Domain.Entities.Customer> _customerRepo;

    public GetSalesSummaryReportQueryHandler(
        IRepository<Domain.Entities.SalesOrder, long> soRepo,
        IRepository<Domain.Entities.DeliveryNote, long> dnRepo,
        IRepository<Domain.Entities.CustomerInvoice, long> invRepo,
        IRepository<Domain.Entities.Customer> customerRepo)
    {
        _soRepo = soRepo;
        _dnRepo = dnRepo;
        _invRepo = invRepo;
        _customerRepo = customerRepo;
    }

    public async Task<ApiResponse<SalesSummaryReportDto>> Handle(
        GetSalesSummaryReportQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var to = request.ToDate ?? today;
        var from = request.FromDate ?? to.AddDays(-30);

        // ─── SO aggregation ─────────────────────────────────────────────────
        // Per-SO total is itself a SUM over Lines, so SQL Server rejects an outer
        // SUM(SUM(..)) in the same statement ("Cannot perform an aggregate function
        // on an expression containing an aggregate or a subquery"). We materialize
        // the per-SO projection first, then group by customer in memory.
        var soBase = _soRepo.Query()
            .Where(s => s.OrderDate >= from && s.OrderDate <= to
                     && s.Status != Domain.Entities.SalesOrderStatus.Draft
                     && s.Status != Domain.Entities.SalesOrderStatus.Cancelled);
        if (request.CustomerId.HasValue)
            soBase = soBase.Where(s => s.CustomerId == request.CustomerId.Value);

        var soFlat = await soBase
            .Select(s => new
            {
                s.CustomerId,
                Total = s.Lines.Sum(l => l.Quantity * l.UnitPrice),
                s.ExchangeRate
            })
            .ToListAsync(cancellationToken);

        var soAgg = soFlat
            .GroupBy(x => x.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Count = g.Count(),
                Total = g.Sum(x => x.Total * x.ExchangeRate)   // → BDT
            })
            .ToList();

        // ─── DN aggregation ─────────────────────────────────────────────────
        // Same nested-aggregate issue — materialize per-DN totals, then group.
        var dnBase = _dnRepo.Query()
            .Where(d => d.DispatchDate >= from && d.DispatchDate <= to
                     && d.Status == Domain.Entities.DeliveryNoteStatus.Posted);
        if (request.CustomerId.HasValue)
            dnBase = dnBase.Where(d => d.SalesOrder.CustomerId == request.CustomerId.Value);

        var dnFlat = await dnBase
            .Select(d => new
            {
                CustomerId = d.SalesOrder.CustomerId,
                Value = d.Lines.Sum(l => l.DispatchedQuantity * l.SalesOrderLine.UnitPrice),
                ExchangeRate = d.SalesOrder.ExchangeRate
            })
            .ToListAsync(cancellationToken);

        var dnAgg = dnFlat
            .GroupBy(x => x.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Count = g.Count(),
                Value = g.Sum(x => x.Value * x.ExchangeRate)   // → BDT
            })
            .ToList();

        // ─── Invoice aggregation ────────────────────────────────────────────
        var invBase = _invRepo.Query()
            .Where(i => i.InvoiceDate >= from && i.InvoiceDate <= to
                     && i.Status != Domain.Entities.CustomerInvoiceStatus.Draft
                     && i.Status != Domain.Entities.CustomerInvoiceStatus.Cancelled);
        if (request.CustomerId.HasValue)
            invBase = invBase.Where(i => i.CustomerId == request.CustomerId.Value);

        var invAgg = await invBase
            .GroupBy(i => i.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                Count = g.Count(),
                // Convert each invoice to base currency (BDT) before summing across the customer.
                InvoicedNet = g.Sum(x => x.SubtotalAmount * x.ExchangeRate),
                VatCollected = g.Sum(x => x.VatAmount * x.ExchangeRate),
                InvoicedTotal = g.Sum(x => x.TotalAmount * x.ExchangeRate),
                AmountPaid = g.Sum(x => x.AmountPaid * x.ExchangeRate),
                AmountDue = g.Sum(x => (x.TotalAmount - x.AmountPaid) * x.ExchangeRate)
            })
            .ToListAsync(cancellationToken);

        // ─── Customer lookup (for code/name display) ────────────────────────
        var allCustomerIds = soAgg.Select(x => x.CustomerId)
            .Union(dnAgg.Select(x => x.CustomerId))
            .Union(invAgg.Select(x => x.CustomerId))
            .ToList();

        var customers = await _customerRepo.Query()
            .Where(c => allCustomerIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Code, c.Name })
            .ToListAsync(cancellationToken);

        var soMap = soAgg.ToDictionary(x => x.CustomerId);
        var dnMap = dnAgg.ToDictionary(x => x.CustomerId);
        var invMap = invAgg.ToDictionary(x => x.CustomerId);

        var rows = customers
            .Select(c =>
            {
                var so = soMap.TryGetValue(c.Id, out var s) ? s : null;
                var dn = dnMap.TryGetValue(c.Id, out var d) ? d : null;
                var inv = invMap.TryGetValue(c.Id, out var i) ? i : null;
                return new SalesSummaryRowDto(
                    c.Id, c.Code, c.Name,
                    so?.Count ?? 0, so?.Total ?? 0m,
                    dn?.Count ?? 0, dn?.Value ?? 0m,
                    inv?.Count ?? 0,
                    inv?.InvoicedNet ?? 0m,
                    inv?.VatCollected ?? 0m,
                    inv?.InvoicedTotal ?? 0m,
                    inv?.AmountPaid ?? 0m, inv?.AmountDue ?? 0m);
            })
            .OrderByDescending(r => r.InvoicedTotal)
            .ThenByDescending(r => r.SalesOrderTotal)
            .ToList();

        string? filterCustomerName = null;
        if (request.CustomerId.HasValue)
            filterCustomerName = customers.FirstOrDefault(c => c.Id == request.CustomerId.Value)?.Name;

        var report = new SalesSummaryReportDto(
            FromDate: from,
            ToDate: to,
            CustomerId: request.CustomerId,
            CustomerName: filterCustomerName,
            CustomerCount: rows.Count,
            SalesOrderCount:    rows.Sum(r => r.SalesOrderCount),
            SalesOrderTotal:    rows.Sum(r => r.SalesOrderTotal),
            DeliveryNoteCount:  rows.Sum(r => r.DeliveryNoteCount),
            DeliveryNoteValue:  rows.Sum(r => r.DeliveryNoteValue),
            InvoiceCount:       rows.Sum(r => r.InvoiceCount),
            InvoicedNet:        rows.Sum(r => r.InvoicedNet),
            VatCollected:       rows.Sum(r => r.VatCollected),
            InvoicedTotal:      rows.Sum(r => r.InvoicedTotal),
            AmountCollected:    rows.Sum(r => r.AmountCollected),
            AmountOutstanding:  rows.Sum(r => r.AmountOutstanding),
            Rows: rows);

        return ApiResponse<SalesSummaryReportDto>.Ok(report);
    }
}
