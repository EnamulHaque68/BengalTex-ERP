using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Inventory.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Inventory.Queries;

public sealed record GetStockAdjustmentsQuery(
    PagedQueryParameters Parameters,
    int? WarehouseId = null,
    string? Status = null
) : IRequest<ApiResponse<PagedResult<StockAdjustmentListItemDto>>>;

internal sealed class GetStockAdjustmentsQueryHandler
    : IRequestHandler<GetStockAdjustmentsQuery, ApiResponse<PagedResult<StockAdjustmentListItemDto>>>
{
    private readonly IRepository<Domain.Entities.StockAdjustment, long> _repo;

    public GetStockAdjustmentsQueryHandler(IRepository<Domain.Entities.StockAdjustment, long> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<StockAdjustmentListItemDto>>> Handle(
        GetStockAdjustmentsQuery request, CancellationToken cancellationToken)
    {
        var query = _repo.Query();

        if (request.WarehouseId.HasValue)
            query = query.Where(a => a.WarehouseId == request.WarehouseId.Value);
        if (!string.IsNullOrEmpty(request.Status)
            && Enum.TryParse<Domain.Entities.StockAdjustmentStatus>(request.Status, out var status))
        {
            query = query.Where(a => a.Status == status);
        }

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(a =>
                a.Code.Contains(search) ||
                a.Reason.Contains(search));
        }

        query = (request.Parameters.SortBy?.ToLowerInvariant(), request.Parameters.SortDirection?.ToLowerInvariant()) switch
        {
            ("code", "desc")   => query.OrderByDescending(a => a.Code),
            ("code", _)        => query.OrderBy(a => a.Code),
            ("date", "asc")    => query.OrderBy(a => a.AdjustmentDate),
            ("date", _)        => query.OrderByDescending(a => a.AdjustmentDate),
            ("status", "desc") => query.OrderByDescending(a => a.Status),
            ("status", _)      => query.OrderBy(a => a.Status),
            _                  => query.OrderByDescending(a => a.Id)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .Select(a => new StockAdjustmentListItemDto(
                a.Id, a.Code,
                a.AdjustmentDate,
                a.WarehouseId, a.Warehouse.Name,
                a.Reason, a.Status.ToString(),
                a.Lines.Count))
            .ToListAsync(cancellationToken);

        var result = PagedResult<StockAdjustmentListItemDto>.Create(
            items, request.Parameters.Page, request.Parameters.PageSize, totalCount);
        return ApiResponse<PagedResult<StockAdjustmentListItemDto>>.Ok(result);
    }
}
