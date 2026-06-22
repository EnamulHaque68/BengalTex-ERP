namespace BengalTex.ERP.Application.Attendance.Dtos;

public sealed record MyBreakDto(string? BreakOutTime, string? BreakInTime, int? Minutes);

public sealed record MyAttendanceTodayDto(
    bool HasRecord,
    string? CheckInTime,
    string? CheckOutTime,
    string Status,
    bool IsLate,
    bool IsEarlyLeave,
    int? WorkedMinutes,
    string WorkingHoursLabel,
    bool OnBreak,
    string? LocationStatus,            // "Within Office Area" | "Outside Office Area"
    double? DistanceMeters,
    bool? WithinFence,
    string? MatchedLocationName,
    double? Latitude,
    double? Longitude,
    bool HasSelfie,
    string ApprovalStatus,
    // ── Location & network intelligence (P2) ──
    string? Address,
    string? DeviceType,
    string? Browser,
    string? Os,
    bool? IsProxyVpn,
    string? NetworkNote,
    string? IpAddress,
    string? Network,                  // ISP / connection name
    IReadOnlyList<MyBreakDto> Breaks);

public sealed record MyMonthSummaryDto(
    int Year, int Month, int PresentDays, int AbsentDays, int LeaveDays, int LateDays, decimal OvertimeHours);

public sealed record MyCalendarDayDto(DateOnly Date, string Status);

public sealed record MyAlertDto(string Severity, string Title, string? Detail, string? Time);   // critical|warning|info

public sealed record MyHistoryDto(string Time, string Event, string? Location, string? Remark);

public sealed record MyAttendanceDto(
    int EmployeeId,
    string EmployeeName,
    string EmployeeCode,
    string? Designation,
    string? Department,
    DateOnly Today,
    MyAttendanceTodayDto TodayStatus,
    MyMonthSummaryDto Month,
    IReadOnlyList<MyCalendarDayDto> Calendar,
    IReadOnlyList<MyAlertDto> Alerts,
    IReadOnlyList<MyHistoryDto> History,
    bool RequireSelfie,
    string OfficeStartTime,
    string OfficeEndTime);
