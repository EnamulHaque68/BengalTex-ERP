using BengalTex.ERP.Application.Attendance.Commands;
using BengalTex.ERP.Application.Attendance.Dtos;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Attendance.Queries;

// ── Employee: my own correction requests ──

public sealed record GetMyAttendanceRequestsQuery : IRequest<ApiResponse<IReadOnlyList<AttendanceRequestDto>>>;

internal sealed class GetMyAttendanceRequestsQueryHandler
    : IRequestHandler<GetMyAttendanceRequestsQuery, ApiResponse<IReadOnlyList<AttendanceRequestDto>>>
{
    private readonly IRepository<AttendanceRequest, long> _repo;
    private readonly IRepository<Domain.Entities.Employee> _employeeRepo;
    private readonly ICurrentUserService _currentUser;

    public GetMyAttendanceRequestsQueryHandler(
        IRepository<AttendanceRequest, long> repo, IRepository<Domain.Entities.Employee> employeeRepo, ICurrentUserService currentUser)
    { _repo = repo; _employeeRepo = employeeRepo; _currentUser = currentUser; }

    public async Task<ApiResponse<IReadOnlyList<AttendanceRequestDto>>> Handle(GetMyAttendanceRequestsQuery req, CancellationToken ct)
    {
        var employee = await AttendanceResolver.ResolveAsync(_employeeRepo, _currentUser, ct);
        if (employee is null)
            return ApiResponse<IReadOnlyList<AttendanceRequestDto>>.Fail("Your login isn't linked to an active employee.");

        var list = await _repo.Query().AsNoTracking()
            .Where(r => r.EmployeeId == employee.Id)
            .OrderByDescending(r => r.CreatedAt)
            .Select(AttendanceRequestProjection.MapExpr)
            .ToListAsync(ct);

        return ApiResponse<IReadOnlyList<AttendanceRequestDto>>.Ok(list);
    }
}

// ── Supervisor: my team's requests (default Pending) ──

public sealed record GetTeamAttendanceRequestsQuery(string? Status = "Pending")
    : IRequest<ApiResponse<IReadOnlyList<AttendanceRequestDto>>>;

internal sealed class GetTeamAttendanceRequestsQueryHandler
    : IRequestHandler<GetTeamAttendanceRequestsQuery, ApiResponse<IReadOnlyList<AttendanceRequestDto>>>
{
    private readonly IRepository<AttendanceRequest, long> _repo;
    private readonly IRepository<Domain.Entities.Employee> _employeeRepo;
    private readonly ICurrentUserService _currentUser;

    public GetTeamAttendanceRequestsQueryHandler(
        IRepository<AttendanceRequest, long> repo, IRepository<Domain.Entities.Employee> employeeRepo, ICurrentUserService currentUser)
    { _repo = repo; _employeeRepo = employeeRepo; _currentUser = currentUser; }

    public async Task<ApiResponse<IReadOnlyList<AttendanceRequestDto>>> Handle(GetTeamAttendanceRequestsQuery req, CancellationToken ct)
    {
        var supervisor = await AttendanceResolver.ResolveAsync(_employeeRepo, _currentUser, ct);
        if (supervisor is null)
            return ApiResponse<IReadOnlyList<AttendanceRequestDto>>.Fail("Your login isn't linked to an active employee.");

        var seesAll = AttendanceSupervision.SeesAll(_currentUser);

        var query = _repo.Query().AsNoTracking().AsQueryable();
        if (!seesAll) query = query.Where(r => r.Employee.ReportingToEmployeeId == supervisor.Id);

        if (!string.IsNullOrWhiteSpace(req.Status)
            && Enum.TryParse<AttendanceRequestStatus>(req.Status, out var st))
            query = query.Where(r => r.Status == st);

        var list = await query
            .OrderByDescending(r => r.Status == AttendanceRequestStatus.Pending)
            .ThenByDescending(r => r.CreatedAt)
            .Select(AttendanceRequestProjection.MapExpr)
            .ToListAsync(ct);

        return ApiResponse<IReadOnlyList<AttendanceRequestDto>>.Ok(list);
    }
}

internal static class AttendanceRequestProjection
{
    public static System.Linq.Expressions.Expression<System.Func<AttendanceRequest, AttendanceRequestDto>> MapExpr => r =>
        new AttendanceRequestDto(
            r.Id, r.EmployeeId, r.Employee.Code, r.Employee.FullName,
            r.RequestDate, r.RequestType.ToString(),
            r.RequestedCheckInTime, r.RequestedCheckOutTime,
            r.RequestedStatus != null ? r.RequestedStatus.ToString() : null,
            r.Reason, r.Status.ToString(),
            r.ReviewedByEmployee != null ? r.ReviewedByEmployee.FullName : null,
            r.ReviewedAt, r.ReviewNote, r.CreatedAt);
}
