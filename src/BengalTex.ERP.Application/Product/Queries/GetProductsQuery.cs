using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Product.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Product.Queries;

public sealed record GetProductsQuery(
    PagedQueryParameters Parameters,
    int? CategoryId = null,
    bool IncludeInactive = false
) : IRequest<ApiResponse<PagedResult<ProductListItemDto>>>;

internal sealed class GetProductsQueryHandler
    : IRequestHandler<GetProductsQuery, ApiResponse<PagedResult<ProductListItemDto>>>
{
    private readonly IRepository<Domain.Entities.Product> _repo;

    public GetProductsQueryHandler(IRepository<Domain.Entities.Product> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<ProductListItemDto>>> Handle(
        GetProductsQuery request, CancellationToken cancellationToken)
    {
        var query = _repo.Query();

        if (request.CategoryId.HasValue)
            query = query.Where(p => p.ProductCategoryId == request.CategoryId);

        if (!request.IncludeInactive)
            query = query.Where(p => p.IsActive);

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(p =>
                p.Code.Contains(search) ||
                p.Name.Contains(search) ||
                (p.Specification != null && p.Specification.Contains(search)));
        }

        query = (request.Parameters.SortBy?.ToLowerInvariant(), request.Parameters.SortDirection?.ToLowerInvariant()) switch
        {
            ("code", "desc")  => query.OrderByDescending(p => p.Code),
            ("code", _)       => query.OrderBy(p => p.Code),
            ("name", "desc")  => query.OrderByDescending(p => p.Name),
            ("price", "desc") => query.OrderByDescending(p => p.SalesPrice),
            ("price", _)      => query.OrderBy(p => p.SalesPrice),
            _                 => query.OrderBy(p => p.Name)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .Select(p => new ProductListItemDto(
                p.Id, p.Code, p.Name,
                p.ProductCategory.Name,
                p.UnitOfMeasure.Code,
                p.SalesPrice, p.ReorderLevel, p.WeightedAverageCost, p.IsStockItem, p.IsActive))
            .ToListAsync(cancellationToken);

        var result = PagedResult<ProductListItemDto>.Create(
            items, request.Parameters.Page, request.Parameters.PageSize, totalCount);
        return ApiResponse<PagedResult<ProductListItemDto>>.Ok(result);
    }
}
