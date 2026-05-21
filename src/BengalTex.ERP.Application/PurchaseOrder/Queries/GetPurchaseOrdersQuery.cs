using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.PurchaseOrder.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.PurchaseOrder.Queries;

public sealed record GetPurchaseOrdersQuery(
    PagedQueryParameters Parameters,
    int? SupplierId = null,
    string? Status = null
) : IRequest<ApiResponse<PagedResult<PurchaseOrderListItemDto>>>;

internal sealed class GetPurchaseOrdersQueryHandler
    : IRequestHandler<GetPurchaseOrdersQuery, ApiResponse<PagedResult<PurchaseOrderListItemDto>>>
{
    private readonly IRepository<Domain.Entities.PurchaseOrder, long> _repo;

    public GetPurchaseOrdersQueryHandler(IRepository<Domain.Entities.PurchaseOrder, long> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<PurchaseOrderListItemDto>>> Handle(
        GetPurchaseOrdersQuery request, CancellationToken cancellationToken)
    {
        var query = _repo.Query();

        if (request.SupplierId.HasValue)
            query = query.Where(p => p.SupplierId == request.SupplierId.Value);

        if (!string.IsNullOrEmpty(request.Status)
            && Enum.TryParse<Domain.Entities.PurchaseOrderStatus>(request.Status, out var status))
        {
            query = query.Where(p => p.Status == status);
        }

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(p =>
                p.Code.Contains(search) ||
                p.Supplier.Code.Contains(search) ||
                p.Supplier.Name.Contains(search));
        }

        query = (request.Parameters.SortBy?.ToLowerInvariant(), request.Parameters.SortDirection?.ToLowerInvariant()) switch
        {
            ("code", "desc")     => query.OrderByDescending(p => p.Code),
            ("code", _)          => query.OrderBy(p => p.Code),
            ("supplier", "desc") => query.OrderByDescending(p => p.Supplier.Name),
            ("supplier", _)      => query.OrderBy(p => p.Supplier.Name),
            ("orderdate", "asc") => query.OrderBy(p => p.OrderDate),
            ("orderdate", _)     => query.OrderByDescending(p => p.OrderDate),
            ("status", "desc")   => query.OrderByDescending(p => p.Status),
            ("status", _)        => query.OrderBy(p => p.Status),
            _                    => query.OrderByDescending(p => p.Id)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .Select(p => new PurchaseOrderListItemDto(
                p.Id, p.Code, p.SupplierId, p.Supplier.Name,
                p.OrderDate, p.ExpectedDeliveryDate,
                p.Status.ToString(),
                p.Currency.Code, p.ExchangeRate,
                p.Lines.Count,
                p.Lines.Sum(l => (decimal?)(l.Quantity * l.UnitPrice)) ?? 0m,
                (p.Lines.Sum(l => (decimal?)(l.Quantity * l.UnitPrice)) ?? 0m) * p.ExchangeRate))
            .ToListAsync(cancellationToken);

        var result = PagedResult<PurchaseOrderListItemDto>.Create(
            items, request.Parameters.Page, request.Parameters.PageSize, totalCount);
        return ApiResponse<PagedResult<PurchaseOrderListItemDto>>.Ok(result);
    }
}
