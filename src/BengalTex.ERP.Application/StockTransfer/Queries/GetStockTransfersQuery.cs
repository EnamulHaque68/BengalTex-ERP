using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.StockTransfer.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.StockTransfer.Queries;

public sealed record GetStockTransfersQuery(
    PagedQueryParameters Parameters,
    int? SourceWarehouseId = null,
    int? DestinationWarehouseId = null,
    string? Status = null
) : IRequest<ApiResponse<PagedResult<StockTransferListItemDto>>>;

internal sealed class GetStockTransfersQueryHandler
    : IRequestHandler<GetStockTransfersQuery, ApiResponse<PagedResult<StockTransferListItemDto>>>
{
    private readonly IRepository<Domain.Entities.StockTransfer, long> _repo;

    public GetStockTransfersQueryHandler(IRepository<Domain.Entities.StockTransfer, long> repo)
        => _repo = repo;

    public async Task<ApiResponse<PagedResult<StockTransferListItemDto>>> Handle(
        GetStockTransfersQuery request, CancellationToken cancellationToken)
    {
        var query = _repo.Query();

        if (request.SourceWarehouseId.HasValue)
            query = query.Where(s => s.SourceWarehouseId == request.SourceWarehouseId.Value);
        if (request.DestinationWarehouseId.HasValue)
            query = query.Where(s => s.DestinationWarehouseId == request.DestinationWarehouseId.Value);

        if (!string.IsNullOrEmpty(request.Status)
            && Enum.TryParse<Domain.Entities.StockTransferStatus>(request.Status, out var st))
        {
            query = query.Where(s => s.Status == st);
        }

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(s =>
                s.Code.Contains(search) ||
                s.SourceWarehouse.Name.Contains(search) ||
                s.DestinationWarehouse.Name.Contains(search) ||
                (s.Notes != null && s.Notes.Contains(search)));
        }

        query = (request.Parameters.SortBy?.ToLowerInvariant(), request.Parameters.SortDirection?.ToLowerInvariant()) switch
        {
            ("code", "desc")          => query.OrderByDescending(s => s.Code),
            ("code", _)               => query.OrderBy(s => s.Code),
            ("transferdate", "asc")   => query.OrderBy(s => s.TransferDate),
            ("transferdate", _)       => query.OrderByDescending(s => s.TransferDate),
            _                         => query.OrderByDescending(s => s.Id)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .Select(s => new StockTransferListItemDto(
                s.Id, s.Code,
                s.SourceWarehouseId, s.SourceWarehouse.Name,
                s.DestinationWarehouseId, s.DestinationWarehouse.Name,
                s.TransferDate,
                s.Status.ToString(),
                s.Lines.Count,
                s.Lines.Sum(l => l.Quantity)))
            .ToListAsync(cancellationToken);

        var result = PagedResult<StockTransferListItemDto>.Create(
            items, request.Parameters.Page, request.Parameters.PageSize, totalCount);
        return ApiResponse<PagedResult<StockTransferListItemDto>>.Ok(result);
    }
}
