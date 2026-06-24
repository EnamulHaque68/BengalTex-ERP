using BengalTex.ERP.Application.Attendance.Commands;
using BengalTex.ERP.Application.Attendance.Dtos;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Attendance.Queries;

/// <summary>
/// Supervisor "Team Attendance" — the attendance of the logged-in user's direct reports for a date
/// range (defaults to today), with verification context + computed red flags. Org-wide reviewers
/// (HR/admin) see everyone. Optionally filter to flagged rows only.
/// </summary>
public sealed record GetTeamAttendanceQuery(DateOnly? FromDate, DateOnly? ToDate, bool OnlyFlagged = false)
    : IRequest<ApiResponse<TeamAttendanceDto>>;

internal sealed class GetTeamAttendanceQueryHandler : IRequestHandler<GetTeamAttendanceQuery, ApiResponse<TeamAttendanceDto>>
{
    private readonly IRepository<AttendanceRecord, long> _repo;
    private readonly IRepository<AttendanceRequest, long> _requestRepo;
    private readonly IRepository<Domain.Entities.Employee> _employeeRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public GetTeamAttendanceQueryHandler(
        IRepository<AttendanceRecord, long> repo, IRepository<AttendanceRequest, long> requestRepo,
        IRepository<Domain.Entities.Employee> employeeRepo, ICurrentUserService currentUser, IDateTimeProvider clock)
    { _repo = repo; _requestRepo = requestRepo; _employeeRepo = employeeRepo; _currentUser = currentUser; _clock = clock; }

    public async Task<ApiResponse<TeamAttendanceDto>> Handle(GetTeamAttendanceQuery req, CancellationToken ct)
    {
        var supervisor = await AttendanceResolver.ResolveAsync(_employeeRepo, _currentUser, ct);
        if (supervisor is null)
            return ApiResponse<TeamAttendanceDto>.Fail("Your login isn't linked to an active employee.");

        var seesAll = AttendanceSupervision.SeesAll(_currentUser);
        var today = _clock.Today;
        var from = req.FromDate ?? today;
        var to = req.ToDate ?? today;
        if (to < from) (from, to) = (to, from);

        // Team scope: direct reports (or everyone for org-wide reviewers)
        var teamQuery = _employeeRepo.Query().AsNoTracking().Where(e => e.IsActive && e.Status == EmployeeStatus.Active);
        if (!seesAll) teamQuery = teamQuery.Where(e => e.ReportingToEmployeeId == supervisor.Id);
        var teamIds = await teamQuery.Select(e => e.Id).ToListAsync(ct);
        var teamSize = teamIds.Count;

        if (teamSize == 0)
            return ApiResponse<TeamAttendanceDto>.Ok(new TeamAttendanceDto(
                supervisor.Id, seesAll, new TeamAttendanceSummaryDto(0, 0, 0, 0), Array.Empty<TeamAttendanceRowDto>()));

        var raw = await _repo.Query().AsNoTracking()
            .Where(a => teamIds.Contains(a.EmployeeId) && a.AttendanceDate >= from && a.AttendanceDate <= to)
            .OrderByDescending(a => a.AttendanceDate).ThenBy(a => a.Employee.FullName)
            .Select(a => new
            {
                a.Id, a.EmployeeId, a.Employee.Code, a.Employee.FullName, a.Employee.Designation, a.Employee.Department,
                a.AttendanceDate, a.Status, a.CheckInTime, a.CheckOutTime, a.WorkedMinutes, a.IsLate, a.IsEarlyLeave,
                HasCheckInSelfie = a.CheckInSelfieUrl != null, HasCheckOutSelfie = a.CheckOutSelfieUrl != null,
                a.CheckInWithinFence, a.CheckInDistanceMeters, MatchedLocationName = a.MatchedOfficeLocation!.Name,
                a.CheckInAddress, a.CheckInLatitude, a.CheckInLongitude,
                a.CheckOutWithinFence, a.CheckOutDistanceMeters, a.CheckOutAddress, a.CheckOutLatitude, a.CheckOutLongitude,
                a.CheckInDeviceType, a.CheckInBrowser, a.CheckInOs, a.CheckInIsProxyVpn, a.CheckInNetworkNote, a.CheckInIpAddress,
                a.FaceMatchStatus, a.ApprovalStatus, ApprovedByName = a.ApprovedByEmployee!.FullName, a.ApprovedAt, a.RejectionReason
            })
            .ToListAsync(ct);

        var rows = raw.Select(a =>
        {
            var flags = AttendanceFlags.For(
                a.CheckInWithinFence, a.CheckInIsProxyVpn, a.CheckInNetworkNote,
                a.FaceMatchStatus, a.IsLate, a.HasCheckInSelfie, a.ApprovalStatus);
            return new TeamAttendanceRowDto(
                a.Id, a.EmployeeId, a.Code, a.FullName, a.Designation, a.Department,
                a.AttendanceDate, a.Status.ToString(), a.CheckInTime, a.CheckOutTime,
                a.WorkedMinutes is int m ? $"{m / 60:00}h {m % 60:00}m" : null,
                a.IsLate, a.IsEarlyLeave,
                a.HasCheckInSelfie, a.HasCheckOutSelfie, a.CheckInWithinFence, a.CheckInDistanceMeters,
                a.MatchedLocationName, a.CheckInAddress, a.CheckInLatitude, a.CheckInLongitude,
                a.CheckOutWithinFence, a.CheckOutDistanceMeters, a.CheckOutAddress, a.CheckOutLatitude, a.CheckOutLongitude,
                a.CheckInDeviceType, a.CheckInBrowser, a.CheckInOs, a.CheckInIsProxyVpn, a.CheckInNetworkNote, a.CheckInIpAddress,
                a.ApprovalStatus.ToString(), a.ApprovedByName, a.ApprovedAt, a.RejectionReason, flags);
        }).ToList();

        if (req.OnlyFlagged) rows = rows.Where(r => r.Flags.Count > 0).ToList();

        var presentToday = rows.Count(r => r.AttendanceDate == today && r.Status.ToAttendancePresent());
        var pendingApprovals = rows.Count(r => r.ApprovalStatus == nameof(AttendanceApprovalStatus.Pending));
        var flaggedToday = rows.Count(r => r.AttendanceDate == today && r.Flags.Count > 0);

        var summary = new TeamAttendanceSummaryDto(teamSize, presentToday, pendingApprovals, flaggedToday);
        return ApiResponse<TeamAttendanceDto>.Ok(new TeamAttendanceDto(supervisor.Id, seesAll, summary, rows));
    }
}

internal static class TeamStatusHelpers
{
    public static bool ToAttendancePresent(this string status) =>
        Enum.TryParse<AttendanceStatus>(status, out var s) && s.CountsAsPresent();
}
