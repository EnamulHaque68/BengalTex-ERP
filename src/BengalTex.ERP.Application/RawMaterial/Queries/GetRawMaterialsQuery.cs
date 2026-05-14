using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.RawMaterial.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.RawMaterial.Queries;

public sealed record GetRawMaterialsQuery(
    PagedQueryParameters Parameters,
    string? Category = null,
    bool IncludeInactive = false
) : IRequest<ApiResponse<PagedResult<RawMaterialListItemDto>>>;

internal sealed class GetRawMaterialsQueryHandler
    : IRequestHandler<GetRawMaterialsQuery, ApiResponse<PagedResult<RawMaterialListItemDto>>>
{
    private readonly IRepository<Domain.Entities.RawMaterial> _repo;

    public GetRawMaterialsQueryHandler(IRepository<Domain.Entities.RawMaterial> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<RawMaterialListItemDto>>> Handle(
        GetRawMaterialsQuery request, CancellationToken cancellationToken)
    {
        var query = _repo.Query();

        if (!request.IncludeInactive)
            query = query.Where(r => r.IsActive);

        if (!string.IsNullOrEmpty(request.Category)
            && Enum.TryParse<Domain.Entities.MaterialCategory>(request.Category, out var cat))
        {
            query = query.Where(r => r.Category == cat);
        }

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(r =>
                r.Code.Contains(search) ||
                r.Name.Contains(search) ||
                (r.Specification != null && r.Specification.Contains(search)));
        }

        query = (request.Parameters.SortBy?.ToLowerInvariant(), request.Parameters.SortDirection?.ToLowerInvariant()) switch
        {
            ("code", "desc")     => query.OrderByDescending(r => r.Code),
            ("code", _)          => query.OrderBy(r => r.Code),
            ("name", "desc")     => query.OrderByDescending(r => r.Name),
            ("category", "desc") => query.OrderByDescending(r => r.Category),
            ("category", _)      => query.OrderBy(r => r.Category),
            _                    => query.OrderBy(r => r.Name)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .Select(r => new RawMaterialListItemDto(
                r.Id, r.Code, r.Name,
                r.Category.ToString(),
                r.UnitOfMeasure.Code,
                r.MinimumStockLevel, r.StandardCost,
                r.PreferredSupplier != null ? r.PreferredSupplier.Name : null,
                r.IsActive))
            .ToListAsync(cancellationToken);

        var result = PagedResult<RawMaterialListItemDto>.Create(
            items, request.Parameters.Page, request.Parameters.PageSize, totalCount);
        return ApiResponse<PagedResult<RawMaterialListItemDto>>.Ok(result);
    }
}
