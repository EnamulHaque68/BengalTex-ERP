using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Inventory.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Inventory.Queries;

public sealed record GetStockOnHandQuery(
    PagedQueryParameters Parameters,
    int? WarehouseId = null,
    int? RawMaterialId = null,
    int? ProductId = null,
    string? ItemType = null,             // "RawMaterial" or "Product" filter
    bool BelowMinimumOnly = false
) : IRequest<ApiResponse<PagedResult<StockOnHandDto>>>;

internal sealed class GetStockOnHandQueryHandler
    : IRequestHandler<GetStockOnHandQuery, ApiResponse<PagedResult<StockOnHandDto>>>
{
    private readonly IRepository<Domain.Entities.StockOnHand> _repo;

    public GetStockOnHandQueryHandler(IRepository<Domain.Entities.StockOnHand> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<StockOnHandDto>>> Handle(
        GetStockOnHandQuery request, CancellationToken cancellationToken)
    {
        var query = _repo.Query();

        if (request.WarehouseId.HasValue)
            query = query.Where(s => s.WarehouseId == request.WarehouseId.Value);
        if (request.RawMaterialId.HasValue)
            query = query.Where(s => s.RawMaterialId == request.RawMaterialId.Value);
        if (request.ProductId.HasValue)
            query = query.Where(s => s.ProductId == request.ProductId.Value);

        if (string.Equals(request.ItemType, "RawMaterial", StringComparison.OrdinalIgnoreCase))
            query = query.Where(s => s.RawMaterialId != null);
        else if (string.Equals(request.ItemType, "Product", StringComparison.OrdinalIgnoreCase))
            query = query.Where(s => s.ProductId != null);

        if (request.BelowMinimumOnly)
            query = query.Where(s => s.RawMaterialId != null
                && s.Quantity < s.RawMaterial!.MinimumStockLevel);

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(s =>
                (s.RawMaterial != null && (s.RawMaterial.Code.Contains(search) || s.RawMaterial.Name.Contains(search))) ||
                (s.Product != null && (s.Product.Code.Contains(search) || s.Product.Name.Contains(search))));
        }

        query = (request.Parameters.SortBy?.ToLowerInvariant(), request.Parameters.SortDirection?.ToLowerInvariant()) switch
        {
            ("warehouse", "desc") => query.OrderByDescending(s => s.Warehouse.Code),
            ("warehouse", _)      => query.OrderBy(s => s.Warehouse.Code),
            ("quantity", "asc")   => query.OrderBy(s => s.Quantity),
            ("quantity", _)       => query.OrderByDescending(s => s.Quantity),
            _                     => query.OrderBy(s => s.RawMaterialId != null ? s.RawMaterial!.Name : s.Product!.Name)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .Select(s => new StockOnHandDto(
                s.RawMaterialId != null ? "RawMaterial" : "Product",
                s.RawMaterialId,
                s.ProductId,
                s.RawMaterialId != null ? s.RawMaterial!.Code : s.Product!.Code,
                s.RawMaterialId != null ? s.RawMaterial!.Name : s.Product!.Name,
                s.RawMaterialId != null ? s.RawMaterial!.UnitOfMeasure.Code : s.Product!.UnitOfMeasure.Code,
                s.WarehouseId,
                s.Warehouse.Code,
                s.Warehouse.Name,
                s.Quantity,
                s.RawMaterialId != null ? s.RawMaterial!.MinimumStockLevel : 0m,
                s.RawMaterialId != null && s.Quantity < s.RawMaterial!.MinimumStockLevel))
            .ToListAsync(cancellationToken);

        var result = PagedResult<StockOnHandDto>.Create(
            items, request.Parameters.Page, request.Parameters.PageSize, totalCount);
        return ApiResponse<PagedResult<StockOnHandDto>>.Ok(result);
    }
}
