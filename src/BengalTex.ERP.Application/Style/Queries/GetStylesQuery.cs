using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Style.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Style.Queries;

public sealed record GetStylesQuery(
    PagedQueryParameters Parameters,
    bool IncludeInactive = false,
    int? BuyerId = null,
    string? Status = null
) : IRequest<ApiResponse<PagedResult<StyleListItemDto>>>;

internal sealed class GetStylesQueryHandler
    : IRequestHandler<GetStylesQuery, ApiResponse<PagedResult<StyleListItemDto>>>
{
    private readonly IRepository<Domain.Entities.Style> _repo;

    public GetStylesQueryHandler(IRepository<Domain.Entities.Style> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<StyleListItemDto>>> Handle(
        GetStylesQuery request, CancellationToken ct)
    {
        var query = _repo.Query();

        if (!request.IncludeInactive)
            query = query.Where(s => s.IsActive);
        if (request.BuyerId.HasValue)
            query = query.Where(s => s.BuyerId == request.BuyerId.Value);
        if (!string.IsNullOrEmpty(request.Status)
            && Enum.TryParse<StyleStatus>(request.Status, out var status))
        {
            query = query.Where(s => s.Status == status);
        }

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(s =>
                s.Code.Contains(search) ||
                s.StyleName.Contains(search) ||
                s.Buyer.Name.Contains(search) ||
                (s.BuyerStyleRef != null && s.BuyerStyleRef.Contains(search)));
        }

        query = (request.Parameters.SortBy?.ToLowerInvariant(), request.Parameters.SortDirection?.ToLowerInvariant()) switch
        {
            ("code", "desc") => query.OrderByDescending(s => s.Code),
            ("code", _)      => query.OrderBy(s => s.Code),
            ("name", "desc") => query.OrderByDescending(s => s.StyleName),
            _                => query.OrderBy(s => s.StyleName)
        };

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .Select(s => new StyleListItemDto(
                s.Id, s.Code, s.StyleName, s.Buyer.Name,
                s.Product != null ? s.Product.Name : null,
                s.Season, s.Status.ToString(), s.IsActive))
            .ToListAsync(ct);

        var result = PagedResult<StyleListItemDto>.Create(
            items, request.Parameters.Page, request.Parameters.PageSize, totalCount);
        return ApiResponse<PagedResult<StyleListItemDto>>.Ok(result);
    }
}
