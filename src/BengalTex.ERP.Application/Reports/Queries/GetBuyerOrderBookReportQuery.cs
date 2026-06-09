using BengalTex.ERP.Application.Reports.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Reports.Queries;

/// <summary>
/// "Order Book" snapshot grouped by customer. Active SOs only (not Cancelled, not Closed).
/// Computes per-SO ordered/dispatched/pending qty + base BDT value, plus the customer's
/// outstanding invoice balance across non-Draft, non-Cancelled CustomerInvoices.
///
/// Each SO's "dispatched value" is estimated proportionally: total × (dispatchedQty / orderedQty)
/// — for a more precise figure we'd need to walk DN line totals, but the proportional estimate
/// is what sales teams actually use day-to-day.
///
/// When <paramref name="CustomerId"/> is null, returns rollup for every customer that has at
/// least one active order. Otherwise returns just that one customer's data.
/// </summary>
public sealed record GetBuyerOrderBookReportQuery(int? CustomerId = null)
    : IRequest<ApiResponse<BuyerOrderBookReportDto>>;

internal sealed class GetBuyerOrderBookReportQueryHandler
    : IRequestHandler<GetBuyerOrderBookReportQuery, ApiResponse<BuyerOrderBookReportDto>>
{
    private readonly IRepository<Domain.Entities.SalesOrder, long> _soRepo;
    private readonly IRepository<Domain.Entities.CustomerInvoice, long> _invRepo;

    public GetBuyerOrderBookReportQueryHandler(
        IRepository<Domain.Entities.SalesOrder, long> soRepo,
        IRepository<Domain.Entities.CustomerInvoice, long> invRepo)
    {
        _soRepo = soRepo;
        _invRepo = invRepo;
    }

    public async Task<ApiResponse<BuyerOrderBookReportDto>> Handle(
        GetBuyerOrderBookReportQuery req, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        // ── Active sales orders (with line aggregates) ──
        var soQuery = _soRepo.Query()
            .Where(s => s.Status != SalesOrderStatus.Cancelled
                     && s.Status != SalesOrderStatus.Closed
                     && s.Status != SalesOrderStatus.Draft);
        if (req.CustomerId.HasValue) soQuery = soQuery.Where(s => s.CustomerId == req.CustomerId.Value);

        var soData = await soQuery
            .Select(s => new
            {
                SalesOrderId = s.Id,
                s.Code,
                s.CustomerId,
                CustomerCode = s.Customer.Code,
                CustomerName = s.Customer.Name,
                s.Customer.CreditPeriodDays,
                s.Customer.CreditLimit,
                s.OrderDate,
                s.RequiredDeliveryDate,
                s.CustomerPoRef,
                s.Status,
                CurrencyCode = s.Currency.Code,
                s.ExchangeRate,
                Ordered = s.Lines.Sum(l => l.Quantity),
                Dispatched = s.Lines.Sum(l => l.DispatchedQuantity),
                TotalAmount = s.Lines.Sum(l => l.Quantity * l.UnitPrice)
            })
            .ToListAsync(ct);

        // ── Outstanding customer invoice balance per customer (base BDT) ──
        var customerIds = soData.Select(x => x.CustomerId).Distinct().ToList();
        var invData = await _invRepo.Query()
            .Where(i => customerIds.Contains(i.CustomerId)
                     && i.Status != CustomerInvoiceStatus.Draft
                     && i.Status != CustomerInvoiceStatus.Cancelled)
            .Select(i => new
            {
                i.CustomerId,
                Outstanding = (i.TotalAmount - i.AmountPaid) * i.ExchangeRate
            })
            .ToListAsync(ct);
        var outstandingByCustomer = invData
            .GroupBy(x => x.CustomerId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Outstanding));

        // ── Build per-SO rows ──
        var orderRows = soData.Select(s =>
        {
            var pending = s.Ordered - s.Dispatched;
            var completion = s.Ordered > 0
                ? Math.Round(s.Dispatched / s.Ordered * 100m, 1, MidpointRounding.AwayFromZero)
                : 0m;
            var isOverdue = s.RequiredDeliveryDate.HasValue
                            && s.RequiredDeliveryDate.Value < today
                            && pending > 0m;
            var baseTotal = Math.Round(s.TotalAmount * s.ExchangeRate, 2, MidpointRounding.AwayFromZero);
            return new
            {
                s.CustomerId, s.CustomerCode, s.CustomerName,
                s.CreditPeriodDays, s.CreditLimit,
                Dto = new BuyerOrderBookSalesOrderDto(
                    s.SalesOrderId, s.Code, s.OrderDate, s.RequiredDeliveryDate,
                    s.CustomerPoRef, s.Status.ToString(),
                    s.CurrencyCode, s.ExchangeRate,
                    s.TotalAmount, baseTotal,
                    s.Ordered, s.Dispatched, pending, completion, isOverdue),
                BaseTotal = baseTotal,
                Pending = pending,
                IsOverdue = isOverdue
            };
        }).ToList();

        // ── Group by customer ──
        var customerRows = orderRows
            .GroupBy(x => new { x.CustomerId, x.CustomerCode, x.CustomerName, x.CreditPeriodDays, x.CreditLimit })
            .Select(g =>
            {
                var orders = g.OrderByDescending(x => x.Dto.OrderDate).Select(x => x.Dto).ToList();
                var totalValue = g.Sum(x => x.BaseTotal);
                // Proportional dispatched value: Σ baseTotal × (dispatched/ordered)
                var dispatchedValue = g.Sum(x =>
                    x.Dto.OrderedQuantity > 0
                        ? Math.Round(x.BaseTotal * (x.Dto.DispatchedQuantity / x.Dto.OrderedQuantity),
                                     2, MidpointRounding.AwayFromZero)
                        : 0m);
                var pendingValue = totalValue - dispatchedValue;
                outstandingByCustomer.TryGetValue(g.Key.CustomerId, out var outstanding);
                return new BuyerOrderBookRowDto(
                    g.Key.CustomerId, g.Key.CustomerCode, g.Key.CustomerName,
                    g.Key.CreditPeriodDays, g.Key.CreditLimit,
                    orders.Count,
                    g.Count(x => x.IsOverdue),
                    totalValue, dispatchedValue, pendingValue,
                    Math.Round(outstanding, 2, MidpointRounding.AwayFromZero),
                    orders);
            })
            .OrderByDescending(r => r.TotalOrderValueBdt)
            .ToList();

        var dto = new BuyerOrderBookReportDto(
            today, req.CustomerId,
            customerRows.Count,
            customerRows.Sum(r => r.ActiveOrderCount),
            customerRows.Sum(r => r.OverdueOrderCount),
            customerRows.Sum(r => r.TotalOrderValueBdt),
            customerRows.Sum(r => r.DispatchedValueBdt),
            customerRows.Sum(r => r.PendingValueBdt),
            customerRows.Sum(r => r.OutstandingInvoiceBdt),
            customerRows);

        return ApiResponse<BuyerOrderBookReportDto>.Ok(dto);
    }
}
