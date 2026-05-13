using BengalTex.ERP.Application.Warehouse.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Warehouse.Queries;

public sealed record GetWarehousesQuery(int? FactoryId = null, bool IncludeInactive = false)
    : IRequest<ApiResponse<List<WarehouseDto>>>;

internal sealed class GetWarehousesQueryHandler
    : IRequestHandler<GetWarehousesQuery, ApiResponse<List<WarehouseDto>>>
{
    private readonly IRepository<Domain.Entities.Warehouse> _repo;

    public GetWarehousesQueryHandler(IRepository<Domain.Entities.Warehouse> repo) => _repo = repo;

    public async Task<ApiResponse<List<WarehouseDto>>> Handle(
        GetWarehousesQuery request, CancellationToken cancellationToken)
    {
        var query = _repo.Query();
        if (request.FactoryId.HasValue)
            query = query.Where(w => w.FactoryId == request.FactoryId);
        if (!request.IncludeInactive)
            query = query.Where(w => w.IsActive);

        var list = await query
            .OrderBy(w => w.FactoryId)
            .ThenBy(w => w.Code)
            .Select(w => new WarehouseDto(
                w.Id, w.Code, w.Name,
                w.WarehouseType.ToString(),
                w.Address,
                w.FactoryId,
                w.Factory != null ? w.Factory.Name : null,
                w.IsActive))
            .ToListAsync(cancellationToken);

        return ApiResponse<List<WarehouseDto>>.Ok(list);
    }
}
