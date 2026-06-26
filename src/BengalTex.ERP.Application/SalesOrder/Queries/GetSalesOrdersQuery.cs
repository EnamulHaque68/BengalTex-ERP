using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.SalesOrder.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.SalesOrder.Queries;

public sealed record GetSalesOrdersQuery(
    PagedQueryParameters Parameters,
    int? CustomerId = null,
    string? Status = null
) : IRequest<ApiResponse<PagedResult<SalesOrderListItemDto>>>;

internal sealed class GetSalesOrdersQueryHandler
    : IRequestHandler<GetSalesOrdersQuery, ApiResponse<PagedResult<SalesOrderListItemDto>>>
{
    private readonly IRepository<Domain.Entities.SalesOrder, long> _repo;
    private readonly IRepository<Domain.Entities.ProductionOrder, long> _poRepo;

    public GetSalesOrdersQueryHandler(
        IRepository<Domain.Entities.SalesOrder, long> repo,
        IRepository<Domain.Entities.ProductionOrder, long> poRepo)
    {
        _repo = repo;
        _poRepo = poRepo;
    }

    public async Task<ApiResponse<PagedResult<SalesOrderListItemDto>>> Handle(
        GetSalesOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = _repo.Query();

        if (request.CustomerId.HasValue)
            query = query.Where(s => s.CustomerId == request.CustomerId.Value);

        if (!string.IsNullOrEmpty(request.Status)
            && Enum.TryParse<Domain.Entities.SalesOrderStatus>(request.Status, out var status))
        {
            query = query.Where(s => s.Status == status);
        }

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(s =>
                s.Code.Contains(search) ||
                s.Customer.Code.Contains(search) ||
                s.Customer.Name.Contains(search) ||
                (s.CustomerPoRef != null && s.CustomerPoRef.Contains(search)));
        }

        query = (request.Parameters.SortBy?.ToLowerInvariant(), request.Parameters.SortDirection?.ToLowerInvariant()) switch
        {
            ("code", "desc")     => query.OrderByDescending(s => s.Code),
            ("code", _)          => query.OrderBy(s => s.Code),
            ("customer", "desc") => query.OrderByDescending(s => s.Customer.Name),
            ("customer", _)      => query.OrderBy(s => s.Customer.Name),
            ("orderdate", "asc") => query.OrderBy(s => s.OrderDate),
            ("orderdate", _)     => query.OrderByDescending(s => s.OrderDate),
            ("status", "desc")   => query.OrderByDescending(s => s.Status),
            ("status", _)        => query.OrderBy(s => s.Status),
            _                    => query.OrderByDescending(s => s.Id)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        // Page first into an intermediate (with ordered qty), then layer production progress on top.
        var rows = await query
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .Select(s => new
            {
                s.Id, s.Code, s.CustomerId,
                CustomerName = s.Customer.Name,
                s.OrderDate, s.RequiredDeliveryDate,
                Status = s.Status.ToString(),
                CurrencyCode = s.Currency.Code,
                s.ExchangeRate,
                LineCount = s.Lines.Count,
                TotalAmount = s.Lines.Sum(l => (decimal?)(l.Quantity * l.UnitPrice)) ?? 0m,
                OrderedQuantity = s.Lines.Sum(l => (decimal?)l.Quantity) ?? 0m
            })
            .ToListAsync(cancellationToken);

        // Phase 1 — production progress for the page's SOs (one grouped query, merged in memory).
        var soIds = rows.Select(r => r.Id).ToList();
        var linkedPos = await _poRepo.Query()
            .AsNoTracking()
            .Where(p => p.SalesOrderId != null && soIds.Contains(p.SalesOrderId.Value)
                && p.Status != Domain.Entities.ProductionOrderStatus.Cancelled)
            .Select(p => new { SoId = p.SalesOrderId!.Value, p.Quantity, p.Status })
            .ToListAsync(cancellationToken);

        var bySo = linkedPos
            .GroupBy(x => x.SoId)
            .ToDictionary(
                g => g.Key,
                g => g.Where(x => x.Status == Domain.Entities.ProductionOrderStatus.Completed)
                      .Sum(x => x.Quantity));

        var items = rows.Select(r =>
        {
            var produced = bySo.TryGetValue(r.Id, out var p) ? p : 0m;
            var hasAny = bySo.ContainsKey(r.Id);
            return new SalesOrderListItemDto(
                r.Id, r.Code, r.CustomerId, r.CustomerName,
                r.OrderDate, r.RequiredDeliveryDate, r.Status,
                r.CurrencyCode, r.ExchangeRate, r.LineCount,
                r.TotalAmount, r.TotalAmount * r.ExchangeRate,
                ProductionProgressCalc.Percent(r.OrderedQuantity, produced),
                ProductionProgressCalc.DeriveStatus(r.OrderedQuantity, produced, hasAny));
        }).ToList();

        var result = PagedResult<SalesOrderListItemDto>.Create(
            items, request.Parameters.Page, request.Parameters.PageSize, totalCount);
        return ApiResponse<PagedResult<SalesOrderListItemDto>>.Ok(result);
    }
}
