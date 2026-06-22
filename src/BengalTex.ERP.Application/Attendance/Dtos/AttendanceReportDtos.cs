namespace BengalTex.ERP.Application.Attendance.Dtos;

// ── Daily attendance register ──

public sealed record DailyRegisterRowDto(
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string? Department,
    string Status,                 // record status, or "Absent" / "Holiday" / "WeeklyOff"
    string? CheckInTime,
    string? CheckOutTime,
    string? WorkingHoursLabel,
    bool IsLate,
    bool? WithinFence,
    bool HasRecord);

public sealed record DailyRegisterDto(
    System.DateOnly Date,
    bool IsHoliday,
    string? HolidayName,
    int Total,
    int Present,
    int Absent,
    int Late,
    int OnLeave,
    IReadOnlyList<DailyRegisterRowDto> Rows);

// ── Monthly attendance summary ──

public sealed record MonthlySummaryRowDto(
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string? Department,
    int PresentDays,
    int AbsentDays,
    int LateDays,
    int LeaveDays,
    int HolidayWorkDays,
    int OffdayWorkDays,
    decimal OvertimeHours,
    string TotalWorkedLabel);

public sealed record MonthlySummaryDto(
    int Year,
    int Month,
    int WorkingEmployees,
    IReadOnlyList<MonthlySummaryRowDto> Rows);

// ── Exceptions report (late / absent / outside-fence / vpn / missing-checkout / overtime / pending) ──

public sealed record AttendanceExceptionRowDto(
    long Id,
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string? Department,
    System.DateOnly AttendanceDate,
    string Status,
    string? CheckInTime,
    string? CheckOutTime,
    string ExceptionType,
    string Detail);

public sealed record AttendanceExceptionsDto(
    System.DateOnly FromDate,
    System.DateOnly ToDate,
    string Type,
    int Count,
    IReadOnlyList<AttendanceExceptionRowDto> Rows);
