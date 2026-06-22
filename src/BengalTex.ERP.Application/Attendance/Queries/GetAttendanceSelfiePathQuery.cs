using BengalTex.ERP.Application.Attendance.Commands;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Attendance.Queries;

/// <summary>
/// Resolves the stored selfie path for an attendance row, enforcing access: the employee can see
/// their own selfie; a direct supervisor or org-wide reviewer can see their team's. Used by the
/// controller to stream the image. <paramref name="Which"/> = "in" (check-in) or "out" (check-out).
/// </summary>
public sealed record GetAttendanceSelfiePathQuery(long AttendanceId, string Which) : IRequest<ApiResponse<string>>;

internal sealed class GetAttendanceSelfiePathQueryHandler : IRequestHandler<GetAttendanceSelfiePathQuery, ApiResponse<string>>
{
    private readonly IRepository<AttendanceRecord, long> _repo;
    private readonly IRepository<Domain.Entities.Employee> _employeeRepo;
    private readonly ICurrentUserService _currentUser;

    public GetAttendanceSelfiePathQueryHandler(
        IRepository<AttendanceRecord, long> repo, IRepository<Domain.Entities.Employee> employeeRepo, ICurrentUserService currentUser)
    { _repo = repo; _employeeRepo = employeeRepo; _currentUser = currentUser; }

    public async Task<ApiResponse<string>> Handle(GetAttendanceSelfiePathQuery req, CancellationToken ct)
    {
        var viewer = await AttendanceResolver.ResolveAsync(_employeeRepo, _currentUser, ct);
        if (viewer is null) return ApiResponse<string>.Fail("Your login isn't linked to an active employee.");

        var rec = await _repo.Query().AsNoTracking().Include(a => a.Employee)
            .FirstOrDefaultAsync(a => a.Id == req.AttendanceId, ct);
        if (rec is null) return ApiResponse<string>.Fail("Attendance record not found.");

        var isOwner = rec.EmployeeId == viewer.Id;
        var isSupervisor = rec.Employee.ReportingToEmployeeId == viewer.Id;
        if (!isOwner && !isSupervisor && !AttendanceSupervision.SeesAll(_currentUser))
            return ApiResponse<string>.Fail("You're not allowed to view this selfie.");

        var path = string.Equals(req.Which, "out", StringComparison.OrdinalIgnoreCase)
            ? rec.CheckOutSelfieUrl : rec.CheckInSelfieUrl;

        return string.IsNullOrEmpty(path)
            ? ApiResponse<string>.Fail("No selfie on this record.")
            : ApiResponse<string>.Ok(path);
    }
}
