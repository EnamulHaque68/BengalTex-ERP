using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Leaves.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Leaves.Queries;

public sealed record GetLeaveApplicationsQuery(
    PagedQueryParameters Parameters,
    string? Status = null,
    int? EmployeeId = null,
    int? LeaveTypeId = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null
) : IRequest<ApiResponse<PagedResult<LeaveApplicationListItemDto>>>;

internal sealed class GetLeaveApplicationsQueryHandler
    : IRequestHandler<GetLeaveApplicationsQuery, ApiResponse<PagedResult<LeaveApplicationListItemDto>>>
{
    private readonly IRepository<LeaveApplication, long> _repo;
    public GetLeaveApplicationsQueryHandler(IRepository<LeaveApplication, long> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<LeaveApplicationListItemDto>>> Handle(
        GetLeaveApplicationsQuery request, CancellationToken ct)
    {
        var query = _repo.Query();

        if (!string.IsNullOrEmpty(request.Status)
            && Enum.TryParse<LeaveApplicationStatus>(request.Status, out var s))
            query = query.Where(a => a.Status == s);
        if (request.EmployeeId.HasValue) query = query.Where(a => a.EmployeeId == request.EmployeeId.Value);
        if (request.LeaveTypeId.HasValue) query = query.Where(a => a.LeaveTypeId == request.LeaveTypeId.Value);
        if (request.FromDate.HasValue) query = query.Where(a => a.ToDate >= request.FromDate.Value);
        if (request.ToDate.HasValue) query = query.Where(a => a.FromDate <= request.ToDate.Value);

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(a =>
                a.Code.Contains(search) ||
                a.Employee.Code.Contains(search) ||
                a.Employee.FullName.Contains(search));
        }

        query = query.OrderByDescending(a => a.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .Select(a => new LeaveApplicationListItemDto(
                a.Id, a.Code, a.EmployeeId, a.Employee.Code, a.Employee.FullName,
                a.LeaveType.Code, a.LeaveType.Name,
                a.FromDate, a.ToDate, a.TotalDays,
                a.Status.ToString(), a.Reason))
            .ToListAsync(ct);

        var result = PagedResult<LeaveApplicationListItemDto>.Create(
            items, request.Parameters.Page, request.Parameters.PageSize, totalCount);
        return ApiResponse<PagedResult<LeaveApplicationListItemDto>>.Ok(result);
    }
}

public sealed record GetLeaveApplicationByIdQuery(long Id) : IRequest<ApiResponse<LeaveApplicationDto>>;

internal sealed class GetLeaveApplicationByIdQueryHandler
    : IRequestHandler<GetLeaveApplicationByIdQuery, ApiResponse<LeaveApplicationDto>>
{
    private readonly IRepository<LeaveApplication, long> _repo;
    public GetLeaveApplicationByIdQueryHandler(IRepository<LeaveApplication, long> repo) => _repo = repo;

    public async Task<ApiResponse<LeaveApplicationDto>> Handle(GetLeaveApplicationByIdQuery request, CancellationToken ct)
    {
        var dto = await _repo.Query()
            .AsNoTracking()
            .Where(a => a.Id == request.Id)
            .Select(a => new LeaveApplicationDto(
                a.Id, a.Code,
                a.EmployeeId, a.Employee.Code, a.Employee.FullName,
                a.LeaveTypeId, a.LeaveType.Code, a.LeaveType.Name,
                a.FromDate, a.ToDate, a.TotalDays,
                a.Reason, a.Status.ToString(),
                a.DecidedAt, a.DecidedBy, a.RejectionReason,
                a.WriteAttendance, a.Notes))
            .FirstOrDefaultAsync(ct);
        return dto is null
            ? ApiResponse<LeaveApplicationDto>.Fail("Leave application not found.")
            : ApiResponse<LeaveApplicationDto>.Ok(dto);
    }
}
