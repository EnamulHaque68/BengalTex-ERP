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

    public GetSalesOrdersQueryHandler(IRepository<Domain.Entities.SalesOrder, long> repo) => _repo = repo;

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
        var items = await query
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .Select(s => new SalesOrderListItemDto(
                s.Id, s.Code, s.CustomerId, s.Customer.Name,
                s.OrderDate, s.RequiredDeliveryDate,
                s.Status.ToString(),
                s.Lines.Count,
                s.Lines.Sum(l => (decimal?)(l.Quantity * l.UnitPrice)) ?? 0m))
            .ToListAsync(cancellationToken);

        var result = PagedResult<SalesOrderListItemDto>.Create(
            items, request.Parameters.Page, request.Parameters.PageSize, totalCount);
        return ApiResponse<PagedResult<SalesOrderListItemDto>>.Ok(result);
    }
}
