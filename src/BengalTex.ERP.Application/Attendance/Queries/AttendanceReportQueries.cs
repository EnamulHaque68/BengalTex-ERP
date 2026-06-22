using BengalTex.ERP.Application.Attendance.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Attendance.Queries;

internal static class HoursFormat
{
    public static string Hm(int minutes) => $"{minutes / 60:00}h {minutes % 60:00}m";
}

// ════════════════ Daily attendance register ════════════════

public sealed record GetAttendanceDailyRegisterQuery(DateOnly Date) : IRequest<ApiResponse<DailyRegisterDto>>;

internal sealed class GetAttendanceDailyRegisterQueryHandler
    : IRequestHandler<GetAttendanceDailyRegisterQuery, ApiResponse<DailyRegisterDto>>
{
    private readonly IRepository<AttendanceRecord, long> _repo;
    private readonly IRepository<Domain.Entities.Employee> _employeeRepo;
    private readonly IRepository<Holiday> _holidayRepo;
    private readonly IRepository<Shift> _shiftRepo;

    public GetAttendanceDailyRegisterQueryHandler(
        IRepository<AttendanceRecord, long> repo, IRepository<Domain.Entities.Employee> employeeRepo,
        IRepository<Holiday> holidayRepo, IRepository<Shift> shiftRepo)
    { _repo = repo; _employeeRepo = employeeRepo; _holidayRepo = holidayRepo; _shiftRepo = shiftRepo; }

    public async Task<ApiResponse<DailyRegisterDto>> Handle(GetAttendanceDailyRegisterQuery req, CancellationToken ct)
    {
        var holiday = await _holidayRepo.Query().AsNoTracking()
            .FirstOrDefaultAsync(h => h.IsActive && h.Date == req.Date, ct);
        var shifts = await _shiftRepo.Query().AsNoTracking()
            .ToDictionaryAsync(s => s.Id, s => new { s.WeekendDayOfWeek, s.SecondWeekendDayOfWeek }, ct);

        var employees = await _employeeRepo.Query().AsNoTracking()
            .Where(e => e.IsActive && e.Status == EmployeeStatus.Active)
            .OrderBy(e => e.FullName)
            .Select(e => new { e.Id, e.Code, e.FullName, e.Department, e.ShiftId })
            .ToListAsync(ct);

        var records = await _repo.Query().AsNoTracking()
            .Where(a => a.AttendanceDate == req.Date)
            .Select(a => new { a.EmployeeId, a.Status, a.CheckInTime, a.CheckOutTime, a.WorkedMinutes, a.IsLate, a.CheckInWithinFence })
            .ToListAsync(ct);
        var byEmp = records.ToDictionary(r => r.EmployeeId);

        var rows = new List<DailyRegisterRowDto>();
        foreach (var e in employees)
        {
            if (byEmp.TryGetValue(e.Id, out var r))
            {
                rows.Add(new DailyRegisterRowDto(e.Id, e.Code, e.FullName, e.Department, r.Status.ToString(),
                    r.CheckInTime, r.CheckOutTime, r.WorkedMinutes is int m ? HoursFormat.Hm(m) : null,
                    r.IsLate, r.CheckInWithinFence, true));
            }
            else
            {
                string status = holiday is not null ? "Holiday" : IsWeeklyOff(e.ShiftId) ? "WeeklyOff" : "Absent";
                rows.Add(new DailyRegisterRowDto(e.Id, e.Code, e.FullName, e.Department, status,
                    null, null, null, false, null, false));
            }
        }

        var present = rows.Count(r => r.HasRecord && IsPresent(r.Status));
        var absent = rows.Count(r => r.Status == "Absent");
        var late = rows.Count(r => r.IsLate);
        var onLeave = rows.Count(r => r.Status == nameof(AttendanceStatus.Leave));

        var dto = new DailyRegisterDto(req.Date, holiday is not null, holiday?.Name,
            rows.Count, present, absent, late, onLeave, rows);
        return ApiResponse<DailyRegisterDto>.Ok(dto);

        bool IsWeeklyOff(int? shiftId)
        {
            if (shiftId is null || !shifts.TryGetValue(shiftId.Value, out var s)) return false;
            return req.Date.DayOfWeek == s.WeekendDayOfWeek
                || (s.SecondWeekendDayOfWeek.HasValue && req.Date.DayOfWeek == s.SecondWeekendDayOfWeek.Value);
        }
    }

    private static bool IsPresent(string status) =>
        Enum.TryParse<AttendanceStatus>(status, out var s) && s.CountsAsPresent();
}

// ════════════════ Monthly attendance summary ════════════════

public sealed record GetAttendanceMonthlySummaryQuery(int Year, int Month, int? EmployeeId = null)
    : IRequest<ApiResponse<MonthlySummaryDto>>;

internal sealed class GetAttendanceMonthlySummaryQueryHandler
    : IRequestHandler<GetAttendanceMonthlySummaryQuery, ApiResponse<MonthlySummaryDto>>
{
    private readonly IRepository<AttendanceRecord, long> _repo;
    private readonly IRepository<Domain.Entities.Employee> _employeeRepo;

    public GetAttendanceMonthlySummaryQueryHandler(
        IRepository<AttendanceRecord, long> repo, IRepository<Domain.Entities.Employee> employeeRepo)
    { _repo = repo; _employeeRepo = employeeRepo; }

    public async Task<ApiResponse<MonthlySummaryDto>> Handle(GetAttendanceMonthlySummaryQuery req, CancellationToken ct)
    {
        var monthStart = new DateOnly(req.Year, req.Month, 1);
        var monthEnd = monthStart.AddMonths(1);

        var employees = await _employeeRepo.Query().AsNoTracking()
            .Where(e => e.IsActive && e.Status == EmployeeStatus.Active && (req.EmployeeId == null || e.Id == req.EmployeeId))
            .OrderBy(e => e.FullName)
            .Select(e => new { e.Id, e.Code, e.FullName, e.Department })
            .ToListAsync(ct);

        var records = await _repo.Query().AsNoTracking()
            .Where(a => a.AttendanceDate >= monthStart && a.AttendanceDate < monthEnd
                && (req.EmployeeId == null || a.EmployeeId == req.EmployeeId))
            .Select(a => new { a.EmployeeId, a.Status, a.IsLate, a.IsHolidayWork, a.IsOffdayWork, a.OvertimeHours, a.WorkedMinutes })
            .ToListAsync(ct);
        var byEmp = records.ToLookup(r => r.EmployeeId);

        var rows = employees.Select(e =>
        {
            var recs = byEmp[e.Id].ToList();
            return new MonthlySummaryRowDto(
                e.Id, e.Code, e.FullName, e.Department,
                PresentDays: recs.Count(r => r.Status.CountsAsPresent()),
                AbsentDays: recs.Count(r => r.Status == AttendanceStatus.Absent),
                LateDays: recs.Count(r => r.IsLate),
                LeaveDays: recs.Count(r => r.Status == AttendanceStatus.Leave),
                HolidayWorkDays: recs.Count(r => r.IsHolidayWork),
                OffdayWorkDays: recs.Count(r => r.IsOffdayWork),
                OvertimeHours: recs.Sum(r => r.OvertimeHours),
                TotalWorkedLabel: HoursFormat.Hm(recs.Sum(r => r.WorkedMinutes ?? 0)));
        }).ToList();

        return ApiResponse<MonthlySummaryDto>.Ok(new MonthlySummaryDto(req.Year, req.Month, rows.Count, rows));
    }
}

// ════════════════ Exceptions report ════════════════

public sealed record GetAttendanceExceptionsQuery(DateOnly FromDate, DateOnly ToDate, string Type)
    : IRequest<ApiResponse<AttendanceExceptionsDto>>;

internal sealed class GetAttendanceExceptionsQueryHandler
    : IRequestHandler<GetAttendanceExceptionsQuery, ApiResponse<AttendanceExceptionsDto>>
{
    private readonly IRepository<AttendanceRecord, long> _repo;

    public GetAttendanceExceptionsQueryHandler(IRepository<AttendanceRecord, long> repo) => _repo = repo;

    public async Task<ApiResponse<AttendanceExceptionsDto>> Handle(GetAttendanceExceptionsQuery req, CancellationToken ct)
    {
        var from = req.FromDate; var to = req.ToDate;
        if (to < from) (from, to) = (to, from);

        var rows = await _repo.Query().AsNoTracking()
            .Where(a => a.AttendanceDate >= from && a.AttendanceDate <= to)
            .OrderByDescending(a => a.AttendanceDate).ThenBy(a => a.Employee.FullName)
            .Select(a => new
            {
                a.Id, a.EmployeeId, a.Employee.Code, a.Employee.FullName, a.Employee.Department,
                a.AttendanceDate, a.Status, a.CheckInTime, a.CheckOutTime, a.IsLate,
                a.CheckInWithinFence, a.CheckInDistanceMeters, a.CheckInIsProxyVpn, a.CheckInNetworkNote,
                a.OvertimeHours, a.ApprovalStatus
            })
            .ToListAsync(ct);

        var type = req.Type;
        var result = new List<AttendanceExceptionRowDto>();
        foreach (var a in rows)
        {
            string? exType = null, detail = "";
            switch (type)
            {
                case "Late" when a.IsLate: exType = "Late"; detail = $"Checked in {a.CheckInTime}"; break;
                case "OutsideFence" when a.CheckInWithinFence == false:
                    exType = "OutsideFence"; detail = a.CheckInDistanceMeters is double d ? $"{d:0} m from office" : "Outside office area"; break;
                case "ProxyVpn" when a.CheckInIsProxyVpn == true:
                    exType = "ProxyVpn"; detail = a.CheckInNetworkNote ?? "VPN / Proxy"; break;
                case "MissingCheckout" when !string.IsNullOrEmpty(a.CheckInTime) && string.IsNullOrEmpty(a.CheckOutTime):
                    exType = "MissingCheckout"; detail = "No check-out recorded"; break;
                case "Overtime" when a.OvertimeHours > 0:
                    exType = "Overtime"; detail = $"{a.OvertimeHours:0.##} OT hours"; break;
                case "PendingApproval" when a.ApprovalStatus == AttendanceApprovalStatus.Pending:
                    exType = "PendingApproval"; detail = "Awaiting supervisor review"; break;
                case "Absent" when a.Status == AttendanceStatus.Absent:
                    exType = "Absent"; detail = "Marked absent"; break;
            }
            if (exType is not null)
                result.Add(new AttendanceExceptionRowDto(a.Id, a.EmployeeId, a.Code, a.FullName, a.Department,
                    a.AttendanceDate, a.Status.ToString(), a.CheckInTime, a.CheckOutTime, exType, detail));
        }

        return ApiResponse<AttendanceExceptionsDto>.Ok(
            new AttendanceExceptionsDto(from, to, type, result.Count, result));
    }
}
