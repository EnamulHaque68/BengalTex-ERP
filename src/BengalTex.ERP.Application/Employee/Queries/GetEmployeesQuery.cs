using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Employee.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Employee.Queries;

/// <summary>Paginated employee list with optional search (code, name, designation, department, phone).</summary>
public sealed record GetEmployeesQuery(
    PagedQueryParameters Parameters,
    bool IncludeInactive = false,
    string? Department = null,
    string? Status = null
) : IRequest<ApiResponse<PagedResult<EmployeeListItemDto>>>;

internal sealed class GetEmployeesQueryHandler
    : IRequestHandler<GetEmployeesQuery, ApiResponse<PagedResult<EmployeeListItemDto>>>
{
    private readonly IRepository<Domain.Entities.Employee> _repo;

    public GetEmployeesQueryHandler(IRepository<Domain.Entities.Employee> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<EmployeeListItemDto>>> Handle(
        GetEmployeesQuery request, CancellationToken cancellationToken)
    {
        var query = _repo.Query();

        if (!request.IncludeInactive)
            query = query.Where(e => e.IsActive);

        if (!string.IsNullOrWhiteSpace(request.Department))
            query = query.Where(e => e.Department == request.Department);

        if (!string.IsNullOrEmpty(request.Status)
            && Enum.TryParse<Domain.Entities.EmployeeStatus>(request.Status, out var status))
        {
            query = query.Where(e => e.Status == status);
        }

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(e =>
                e.Code.Contains(search) ||
                e.FullName.Contains(search) ||
                (e.Designation != null && e.Designation.Contains(search)) ||
                (e.Department != null && e.Department.Contains(search)) ||
                (e.Phone != null && e.Phone.Contains(search)));
        }

        query = (request.Parameters.SortBy?.ToLowerInvariant(), request.Parameters.SortDirection?.ToLowerInvariant()) switch
        {
            ("code", "desc")    => query.OrderByDescending(e => e.Code),
            ("code", _)         => query.OrderBy(e => e.Code),
            ("name", "desc")    => query.OrderByDescending(e => e.FullName),
            ("department", "desc") => query.OrderByDescending(e => e.Department),
            ("department", _)   => query.OrderBy(e => e.Department),
            _                   => query.OrderBy(e => e.FullName)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .ProjectToType<EmployeeListItemDto>()
            .ToListAsync(cancellationToken);

        var result = PagedResult<EmployeeListItemDto>.Create(
            items, request.Parameters.Page, request.Parameters.PageSize, totalCount);
        return ApiResponse<PagedResult<EmployeeListItemDto>>.Ok(result);
    }
}
