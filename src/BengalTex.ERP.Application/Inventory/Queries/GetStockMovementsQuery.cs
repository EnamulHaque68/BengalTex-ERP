using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Inventory.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Inventory.Queries;

public sealed record GetStockMovementsQuery(
    PagedQueryParameters Parameters,
    int? WarehouseId = null,
    int? RawMaterialId = null,
    string? MovementType = null,
    string? ReferenceType = null,
    long? ReferenceId = null
) : IRequest<ApiResponse<PagedResult<StockMovementDto>>>;

internal sealed class GetStockMovementsQueryHandler
    : IRequestHandler<GetStockMovementsQuery, ApiResponse<PagedResult<StockMovementDto>>>
{
    private readonly IRepository<Domain.Entities.StockMovement, long> _repo;

    public GetStockMovementsQueryHandler(IRepository<Domain.Entities.StockMovement, long> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<StockMovementDto>>> Handle(
        GetStockMovementsQuery request, CancellationToken cancellationToken)
    {
        var query = _repo.Query();

        if (request.WarehouseId.HasValue)
            query = query.Where(m => m.WarehouseId == request.WarehouseId.Value);
        if (request.RawMaterialId.HasValue)
            query = query.Where(m => m.RawMaterialId == request.RawMaterialId.Value);
        if (!string.IsNullOrEmpty(request.MovementType)
            && Enum.TryParse<Domain.Entities.StockMovementType>(request.MovementType, out var mt))
        {
            query = query.Where(m => m.MovementType == mt);
        }
        if (!string.IsNullOrEmpty(request.ReferenceType))
            query = query.Where(m => m.ReferenceType == request.ReferenceType);
        if (request.ReferenceId.HasValue)
            query = query.Where(m => m.ReferenceId == request.ReferenceId.Value);

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(m =>
                m.Code.Contains(search) ||
                m.RawMaterial.Code.Contains(search) ||
                m.RawMaterial.Name.Contains(search) ||
                (m.ReferenceCode != null && m.ReferenceCode.Contains(search)));
        }

        query = (request.Parameters.SortBy?.ToLowerInvariant(), request.Parameters.SortDirection?.ToLowerInvariant()) switch
        {
            ("date", "asc")    => query.OrderBy(m => m.MovementDate),
            ("date", _)        => query.OrderByDescending(m => m.MovementDate),
            ("code", "desc")   => query.OrderByDescending(m => m.Code),
            ("code", _)        => query.OrderBy(m => m.Code),
            _                  => query.OrderByDescending(m => m.Id)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .Select(m => new StockMovementDto(
                m.Id, m.Code,
                m.RawMaterialId, m.RawMaterial.Code, m.RawMaterial.Name,
                m.RawMaterial.UnitOfMeasure.Code,
                m.WarehouseId, m.Warehouse.Code,
                m.SignedQuantity,
                m.MovementType.ToString(),
                m.ReferenceType, m.ReferenceId, m.ReferenceCode,
                m.MovementDate, m.Notes,
                m.CreatedAt))
            .ToListAsync(cancellationToken);

        var result = PagedResult<StockMovementDto>.Create(
            items, request.Parameters.Page, request.Parameters.PageSize, totalCount);
        return ApiResponse<PagedResult<StockMovementDto>>.Ok(result);
    }
}
