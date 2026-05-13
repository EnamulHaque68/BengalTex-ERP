using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Supplier.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Supplier.Queries;

public sealed record GetSuppliersQuery(
    PagedQueryParameters Parameters,
    bool IncludeInactive = false
) : IRequest<ApiResponse<PagedResult<SupplierListItemDto>>>;

internal sealed class GetSuppliersQueryHandler
    : IRequestHandler<GetSuppliersQuery, ApiResponse<PagedResult<SupplierListItemDto>>>
{
    private readonly IRepository<Domain.Entities.Supplier> _repo;

    public GetSuppliersQueryHandler(IRepository<Domain.Entities.Supplier> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<SupplierListItemDto>>> Handle(
        GetSuppliersQuery request, CancellationToken cancellationToken)
    {
        var query = _repo.Query();

        if (!request.IncludeInactive)
            query = query.Where(s => s.IsActive);

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(s =>
                s.Code.Contains(search) ||
                s.Name.Contains(search) ||
                (s.Phone != null && s.Phone.Contains(search)) ||
                (s.Email != null && s.Email.Contains(search)));
        }

        query = (request.Parameters.SortBy?.ToLowerInvariant(), request.Parameters.SortDirection?.ToLowerInvariant()) switch
        {
            ("code", "desc")   => query.OrderByDescending(s => s.Code),
            ("code", _)        => query.OrderBy(s => s.Code),
            ("name", "desc")   => query.OrderByDescending(s => s.Name),
            ("rating", "desc") => query.OrderByDescending(s => s.Rating),
            ("rating", _)      => query.OrderBy(s => s.Rating),
            _                  => query.OrderBy(s => s.Name)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .ProjectToType<SupplierListItemDto>()
            .ToListAsync(cancellationToken);

        var result = PagedResult<SupplierListItemDto>.Create(
            items, request.Parameters.Page, request.Parameters.PageSize, totalCount);
        return ApiResponse<PagedResult<SupplierListItemDto>>.Ok(result);
    }
}
