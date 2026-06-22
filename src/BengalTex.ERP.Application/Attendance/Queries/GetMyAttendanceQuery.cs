using BengalTex.ERP.Application.Attendance.Commands;
using BengalTex.ERP.Application.Attendance.Dtos;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Attendance.Queries;

/// <summary>The "My Attendance" dashboard for the logged-in user (today status + month + calendar + alerts + history).</summary>
public sealed record GetMyAttendanceQuery : IRequest<ApiResponse<MyAttendanceDto>>;

internal sealed class GetMyAttendanceQueryHandler : IRequestHandler<GetMyAttendanceQuery, ApiResponse<MyAttendanceDto>>
{
    private readonly IRepository<AttendanceRecord, long> _repo;
    private readonly IRepository<Domain.Entities.Employee> _employeeRepo;
    private readonly IRepository<AttendanceSettings> _settingsRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public GetMyAttendanceQueryHandler(
        IRepository<AttendanceRecord, long> repo, IRepository<Domain.Entities.Employee> employeeRepo,
        IRepository<AttendanceSettings> settingsRepo, ICurrentUserService currentUser, IDateTimeProvider clock)
    { _repo = repo; _employeeRepo = employeeRepo; _settingsRepo = settingsRepo; _currentUser = currentUser; _clock = clock; }

    public async Task<ApiResponse<MyAttendanceDto>> Handle(GetMyAttendanceQuery req, CancellationToken ct)
    {
        var employee = await AttendanceResolver.ResolveAsync(_employeeRepo, _currentUser, ct);
        if (employee is null)
            return ApiResponse<MyAttendanceDto>.Fail(
                "Your login isn't linked to an active employee. Ask HR to link your account (Employee → Edit Profile → Login Account).");

        var policy = AttendancePolicyValues.From(await _settingsRepo.Query().AsNoTracking().FirstOrDefaultAsync(ct));
        var today = _clock.Today;
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1);

        var todayRec = await _repo.Query().AsNoTracking().Include(a => a.Breaks).Include(a => a.MatchedOfficeLocation)
            .FirstOrDefaultAsync(a => a.EmployeeId == employee.Id && a.AttendanceDate == today, ct);

        var monthRecs = await _repo.Query().AsNoTracking()
            .Where(a => a.EmployeeId == employee.Id && a.AttendanceDate >= monthStart && a.AttendanceDate < monthEnd)
            .Select(a => new { a.AttendanceDate, a.Status, a.IsLate, a.OvertimeHours })
            .ToListAsync(ct);

        // ── Today ──
        var todayDto = BuildToday(todayRec);

        // ── Month summary ──
        var month = new MyMonthSummaryDto(
            today.Year, today.Month,
            PresentDays: monthRecs.Count(r => r.Status.CountsAsPresent()),
            AbsentDays: monthRecs.Count(r => r.Status == AttendanceStatus.Absent),
            LeaveDays: monthRecs.Count(r => r.Status == AttendanceStatus.Leave),
            LateDays: monthRecs.Count(r => r.IsLate),
            OvertimeHours: monthRecs.Sum(r => r.OvertimeHours));

        var calendar = monthRecs.OrderBy(r => r.AttendanceDate)
            .Select(r => new MyCalendarDayDto(r.AttendanceDate, r.Status.ToString())).ToList();

        // ── Alerts ──
        var alerts = new List<MyAlertDto>();
        if (todayRec is not null)
        {
            if (todayRec.IsLate)
                alerts.Add(new MyAlertDto("critical", "You checked in late",
                    $"Check-in {todayRec.CheckInTime} (office {policy.OfficeStart:HH:mm}, grace {policy.GraceMinutes}m)", todayRec.CheckInTime));
            if (todayRec.CheckInWithinFence == false)
                alerts.Add(new MyAlertDto("warning", "Checked in outside the office area",
                    todayRec.CheckInDistanceMeters is double d ? $"{d:0} m from office" : null, todayRec.CheckInTime));
            else if (todayRec.CheckInWithinFence == true)
                alerts.Add(new MyAlertDto("info", "Location verified — within office area", todayRec.MatchedOfficeLocation?.Name, todayRec.CheckInTime));
            if (todayRec.ApprovalStatus == AttendanceApprovalStatus.Pending)
                alerts.Add(new MyAlertDto("warning", "Pending supervisor approval", "Your check-in is awaiting review", todayRec.CheckInTime));
        }
        else
        {
            alerts.Add(new MyAlertDto("info", "Not checked in yet", "Tap Check In to record your attendance", null));
        }

        // ── History timeline ──
        var history = new List<MyHistoryDto>();
        if (todayRec is not null)
        {
            string? loc = todayRec.CheckInWithinFence switch
            {
                true => todayRec.CheckInDistanceMeters is double dm ? $"Within Office Area ({dm:0}m)" : "Within Office Area",
                false => todayRec.CheckInDistanceMeters is double dm ? $"Outside Office Area ({dm:0}m)" : "Outside Office Area",
                _ => todayRec.MatchedOfficeLocation?.Name
            };
            if (!string.IsNullOrEmpty(todayRec.CheckInTime))
                history.Add(new MyHistoryDto(todayRec.CheckInTime!, "Check In", loc, todayRec.IsLate ? "Late" : "On Time"));
            foreach (var b in todayRec.Breaks.OrderBy(b => b.SortOrder))
            {
                if (!string.IsNullOrEmpty(b.BreakOutTime)) history.Add(new MyHistoryDto(b.BreakOutTime!, "Break Out", "Within Office Area", null));
                if (!string.IsNullOrEmpty(b.BreakInTime)) history.Add(new MyHistoryDto(b.BreakInTime!, "Break In", "Within Office Area", b.Minutes is int m ? $"{m} min" : null));
            }
            if (!string.IsNullOrEmpty(todayRec.CheckOutTime))
            {
                string? outLoc = todayRec.CheckOutWithinFence switch
                {
                    true => todayRec.CheckOutDistanceMeters is double dm ? $"Within Office Area ({dm:0}m)" : "Within Office Area",
                    false => "Outside Office Area",
                    _ => loc
                };
                history.Add(new MyHistoryDto(todayRec.CheckOutTime!, "Check Out", outLoc, todayRec.IsEarlyLeave ? "Early leave" : null));
            }
        }

        var dto = new MyAttendanceDto(
            employee.Id, employee.FullName, employee.Code, employee.Designation, employee.Department,
            today, todayDto, month, calendar, alerts.OrderByDescending(a => a.Time).ToList(), history,
            policy.RequireSelfie, policy.OfficeStart.ToString("HH:mm"), policy.OfficeEnd.ToString("HH:mm"));

        return ApiResponse<MyAttendanceDto>.Ok(dto);
    }

    private MyAttendanceTodayDto BuildToday(AttendanceRecord? rec)
    {
        if (rec is null)
            return new MyAttendanceTodayDto(false, null, null, "Not Checked In", false, false, null, "00h 00m",
                false, null, null, null, null, null, null, false, "NotRequired",
                null, null, null, null, null, null, null, null, Array.Empty<MyBreakDto>());

        var onBreak = rec.Breaks.Any(b => string.IsNullOrEmpty(b.BreakInTime));
        var breakMinutes = rec.Breaks.Where(b => b.Minutes.HasValue).Sum(b => b.Minutes!.Value);

        // Worked minutes: persisted on check-out; otherwise live (now − check-in − completed breaks).
        int? workedMinutes = rec.WorkedMinutes;
        if (workedMinutes is null && !string.IsNullOrEmpty(rec.CheckInTime) && TimeOnly.TryParse(rec.CheckInTime, out var ci))
        {
            var nowTod = TimeOnly.FromDateTime(_clock.UtcNow.ToLocalTime().DateTime);
            workedMinutes = Math.Max(0, (int)(nowTod - ci).TotalMinutes - breakMinutes);
        }

        var locationStatus = rec.CheckInWithinFence switch { true => "Within Office Area", false => "Outside Office Area", _ => null };

        return new MyAttendanceTodayDto(
            true, rec.CheckInTime, rec.CheckOutTime, rec.Status.ToString(), rec.IsLate, rec.IsEarlyLeave,
            workedMinutes, FormatHm(workedMinutes ?? 0), onBreak,
            locationStatus, rec.CheckInDistanceMeters, rec.CheckInWithinFence, rec.MatchedOfficeLocation?.Name,
            rec.CheckInLatitude, rec.CheckInLongitude, !string.IsNullOrEmpty(rec.CheckInSelfieUrl),
            rec.ApprovalStatus.ToString(),
            rec.CheckInAddress, rec.CheckInDeviceType, rec.CheckInBrowser, rec.CheckInOs,
            rec.CheckInIsProxyVpn, rec.CheckInNetworkNote, rec.CheckInIpAddress, rec.CheckInIsp,
            rec.Breaks.OrderBy(b => b.SortOrder).Select(b => new MyBreakDto(b.BreakOutTime, b.BreakInTime, b.Minutes)).ToList());
    }

    private static string FormatHm(int minutes) => $"{minutes / 60:00}h {minutes % 60:00}m";
}
