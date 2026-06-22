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
    bool? CheckInWithinFence,
    // ── Location & network intelligence (P2) ──
    string? CheckInAddress = null,
    string? CheckInIpAddress = null,
    string? CheckInDeviceType = null,
    string? CheckInBrowser = null,
    string? CheckInOs = null,
    bool? CheckInIsProxyVpn = null,
    string? CheckInIsp = null,
    string? CheckInNetworkNote = null);
