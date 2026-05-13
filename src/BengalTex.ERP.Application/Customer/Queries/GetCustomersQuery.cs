using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Customer.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Customer.Queries;

/// <summary>Paginated customer list with optional search (code, name, phone, email).</summary>
public sealed record GetCustomersQuery(
    PagedQueryParameters Parameters,
    bool IncludeInactive = false
) : IRequest<ApiResponse<PagedResult<CustomerListItemDto>>>;

internal sealed class GetCustomersQueryHandler
    : IRequestHandler<GetCustomersQuery, ApiResponse<PagedResult<CustomerListItemDto>>>
{
    private readonly IRepository<Domain.Entities.Customer> _repo;

    public GetCustomersQueryHandler(IRepository<Domain.Entities.Customer> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<CustomerListItemDto>>> Handle(
        GetCustomersQuery request, CancellationToken cancellationToken)
    {
        var query = _repo.Query();

        if (!request.IncludeInactive)
            query = query.Where(c => c.IsActive);

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(c =>
                c.Code.Contains(search) ||
                c.Name.Contains(search) ||
                (c.Phone != null && c.Phone.Contains(search)) ||
                (c.Email != null && c.Email.Contains(search)));
        }

        query = (request.Parameters.SortBy?.ToLowerInvariant(), request.Parameters.SortDirection?.ToLowerInvariant()) switch
        {
            ("code", "desc")     => query.OrderByDescending(c => c.Code),
            ("code", _)          => query.OrderBy(c => c.Code),
            ("name", "desc")     => query.OrderByDescending(c => c.Name),
            ("category", "desc") => query.OrderByDescending(c => c.Category),
            ("category", _)      => query.OrderBy(c => c.Category),
            _                    => query.OrderBy(c => c.Name)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .ProjectToType<CustomerListItemDto>()
            .ToListAsync(cancellationToken);

        var result = PagedResult<CustomerListItemDto>.Create(
            items, request.Parameters.Page, request.Parameters.PageSize, totalCount);
        return ApiResponse<PagedResult<CustomerListItemDto>>.Ok(result);
    }
}
