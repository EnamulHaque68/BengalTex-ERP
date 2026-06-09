namespace BengalTex.ERP.Application.Attendance.Dtos;

/// <summary>An attendance row (used for both list + detail).</summary>
public sealed record AttendanceRecordDto(
    long Id,
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    DateOnly AttendanceDate,
    string Status,                 // enum as string
    string? CheckInTime,
    string? CheckOutTime,
    decimal OvertimeHours,
    string? Notes,
    double? CheckInLatitude,
    double? CheckInLongitude,
    double? CheckInDistanceMeters,
    bool? CheckInWithinFence);
