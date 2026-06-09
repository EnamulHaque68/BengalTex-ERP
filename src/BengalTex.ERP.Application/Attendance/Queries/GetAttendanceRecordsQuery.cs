using BengalTex.ERP.Application.Attendance.Dtos;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Attendance.Queries;

/// <summary>Paginated attendance, filtered by date range, employee, and/or status.</summary>
public sealed record GetAttendanceRecordsQuery(
    PagedQueryParameters Parameters,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    int? EmployeeId = null,
    string? Status = null
) : IRequest<ApiResponse<PagedResult<AttendanceRecordDto>>>;

internal sealed class GetAttendanceRecordsQueryHandler
    : IRequestHandler<GetAttendanceRecordsQuery, ApiResponse<PagedResult<AttendanceRecordDto>>>
{
    private readonly IRepository<AttendanceRecord, long> _repo;

    public GetAttendanceRecordsQueryHandler(IRepository<AttendanceRecord, long> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<AttendanceRecordDto>>> Handle(
        GetAttendanceRecordsQuery request, CancellationToken cancellationToken)
    {
        var query = _repo.Query();

        if (request.FromDate.HasValue)
            query = query.Where(a => a.AttendanceDate >= request.FromDate.Value);
        if (request.ToDate.HasValue)
            query = query.Where(a => a.AttendanceDate <= request.ToDate.Value);
        if (request.EmployeeId.HasValue)
            query = query.Where(a => a.EmployeeId == request.EmployeeId.Value);
        if (!string.IsNullOrEmpty(request.Status)
            && Enum.TryParse<AttendanceStatus>(request.Status, out var status))
        {
            query = query.Where(a => a.Status == status);
        }

        var search = request.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(a =>
                a.Employee.Code.Contains(search) ||
                a.Employee.FullName.Contains(search));
        }

        query = query.OrderByDescending(a => a.AttendanceDate).ThenBy(a => a.Employee.FullName);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Parameters.Page - 1) * request.Parameters.PageSize)
            .Take(request.Parameters.PageSize)
            .Select(a => new AttendanceRecordDto(
                a.Id, a.EmployeeId, a.Employee.Code, a.Employee.FullName,
                a.AttendanceDate, a.Status.ToString(),
                a.CheckInTime, a.CheckOutTime, a.OvertimeHours, a.Notes,
                a.CheckInLatitude, a.CheckInLongitude,
                a.CheckInDistanceMeters, a.CheckInWithinFence))
            .ToListAsync(cancellationToken);

        var result = PagedResult<AttendanceRecordDto>.Create(
            items, request.Parameters.Page, request.Parameters.PageSize, totalCount);
        return ApiResponse<PagedResult<AttendanceRecordDto>>.Ok(result);
    }
}
