using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.StockLots.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.StockLots.Queries;

/// <summary>Paged stock lots with filters (item type, warehouse, supplier, status, expiry).</summary>
public sealed record GetStockLotsQuery(
    PagedQueryParameters Parameters,
    string? ItemType = null,          // "RawMaterial" | "Product"
    int? WarehouseId = null,
    int? SupplierId = null,
    string? Status = null,
    int? ExpiringWithinDays = null,   // ExpiryDate <= today + N (catches already-expired too)
    bool ActiveOnly = false           // CurrentQuantity > 0
) : IRequest<ApiResponse<PagedResult<StockLotDto>>>;

internal sealed class GetStockLotsQueryHandler
    : IRequestHandler<GetStockLotsQuery, ApiResponse<PagedResult<StockLotDto>>>
{
    private readonly IRepository<StockLot, long> _repo;

    public GetStockLotsQueryHandler(IRepository<StockLot, long> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<StockLotDto>>> Handle(
        GetStockLotsQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var query = _repo.Query();

        if (request.ItemType == "RawMaterial") query = query.Where(l => l.RawMaterialId != null);
        else if (request.ItemType == "Product") query = query.Where(l => l.ProductId != null);

        if (request.WarehouseId.HasValue) query = query.Where(l => l.WarehouseId == request.WarehouseId.Value);
        if (request.SupplierId.HasValue) query = query.Where(l => l.SupplierId == request.SupplierId.Value);

        if (!string.IsNullOrEmpty(request.Status)
            && Enum.TryParse<LotStatus>(request.Status, out var status))
            query = query.Where(l => l.Status == status);

        if (request.ExpiringWithinDays.HasValue)
        {
            var cutoff = today.AddDays(request.ExpiringWithinDays.Value);
            query = query.Where(l => l.ExpiryDate != null && l.ExpiryDate <= cutoff);
        }

        if (request.ActiveOnly) query = query.Where(l => l.CurrentQuantity > 0);

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(l =>
                l.Code.Contains(search) ||
                l.LotNumber.Contains(search) ||
                (l.Shade != null && l.Shade.Contains(search)) ||
                (l.RawMaterial != null && (l.RawMaterial.Code.Contains(search) || l.RawMaterial.Name.Contains(search))) ||
                (l.Product != null && (l.Product.Code.Contains(search) || l.Product.Name.Contains(search))));
        }

        query = (request.Parameters.SortBy?.ToLowerInvariant(), request.Parameters.SortDirection?.ToLowerInvariant()) switch
        {
            ("code", "asc")        => query.OrderBy(l => l.Code),
            ("code", _)            => query.OrderByDescending(l => l.Code),
            ("lotnumber", "desc")  => query.OrderByDescending(l => l.LotNumber),
            ("lotnumber", _)       => query.OrderBy(l => l.LotNumber),
            ("expirydate", "desc") => query.OrderByDescending(l => l.ExpiryDate),
            ("expirydate", _)      => query.OrderBy(l => l.ExpiryDate),
            ("receiveddate", "asc")=> query.OrderBy(l => l.ReceivedDate),
            _                      => query.OrderByDescending(l => l.Id)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .Select(l => new StockLotDto(
                l.Id, l.Code, l.LotNumber,
                l.RawMaterialId != null ? "RawMaterial" : "Product",
                l.RawMaterialId != null ? l.RawMaterialId!.Value : l.ProductId!.Value,
                l.RawMaterialId != null ? l.RawMaterial!.Code : l.Product!.Code,
                l.RawMaterialId != null ? l.RawMaterial!.Name : l.Product!.Name,
                l.RawMaterialId != null ? l.RawMaterial!.UnitOfMeasure.Code : l.Product!.UnitOfMeasure.Code,
                l.WarehouseId, l.Warehouse.Name,
                l.SupplierId, l.Supplier != null ? l.Supplier.Name : null,
                l.Shade,
                l.ReceivedDate, l.ManufactureDate, l.ExpiryDate,
                l.InitialQuantity, l.CurrentQuantity,
                l.Status.ToString(),
                l.ExpiryDate != null && l.ExpiryDate < today,
                l.SourceType, l.SourceId, l.SourceCode, l.Notes))
            .ToListAsync(cancellationToken);

        var result = PagedResult<StockLotDto>.Create(
            items, request.Parameters.Page, request.Parameters.PageSize, totalCount);
        return ApiResponse<PagedResult<StockLotDto>>.Ok(result);
    }
}
