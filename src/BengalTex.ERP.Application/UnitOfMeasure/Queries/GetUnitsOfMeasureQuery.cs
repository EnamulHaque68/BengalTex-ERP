using BengalTex.ERP.Application.UnitOfMeasure.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.UnitOfMeasure.Queries;

public sealed record GetUnitsOfMeasureQuery(bool IncludeInactive = false)
    : IRequest<ApiResponse<List<UnitOfMeasureDto>>>;

internal sealed class GetUnitsOfMeasureQueryHandler
    : IRequestHandler<GetUnitsOfMeasureQuery, ApiResponse<List<UnitOfMeasureDto>>>
{
    private readonly IRepository<Domain.Entities.UnitOfMeasure> _repo;

    public GetUnitsOfMeasureQueryHandler(IRepository<Domain.Entities.UnitOfMeasure> repo) => _repo = repo;

    public async Task<ApiResponse<List<UnitOfMeasureDto>>> Handle(
        GetUnitsOfMeasureQuery request, CancellationToken cancellationToken)
    {
        // Manual projection because BaseUnit is a navigation property — we need its Code,
        // not just the FK, so a plain ProjectToType won't bring it across without configuration.
        var query = _repo.Query();
        if (!request.IncludeInactive)
            query = query.Where(u => u.IsActive);

        var list = await query
            .OrderBy(u => u.UnitType)
            .ThenByDescending(u => u.BaseUnitId == null)  // base units first within each type
            .ThenBy(u => u.Code)
            .Select(u => new UnitOfMeasureDto(
                u.Id,
                u.Code,
                u.Name,
                u.Symbol,
                u.UnitType.ToString(),
                u.BaseUnitId,
                u.BaseUnit != null ? u.BaseUnit.Code : null,
                u.ConversionFactor,
                u.IsActive))
            .ToListAsync(cancellationToken);

        return ApiResponse<List<UnitOfMeasureDto>>.Ok(list);
    }
}
