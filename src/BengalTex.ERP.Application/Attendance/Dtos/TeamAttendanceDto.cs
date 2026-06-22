namespace BengalTex.ERP.Application.Attendance.Dtos;

/// <summary>One employee's attendance row in the supervisor team view, with review context + red flags.</summary>
public sealed record TeamAttendanceRowDto(
    long Id,
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string? Designation,
    string? Department,
    System.DateOnly AttendanceDate,
    string Status,
    string? CheckInTime,
    string? CheckOutTime,
    string? WorkingHoursLabel,
    bool IsLate,
    bool IsEarlyLeave,
    // Verification
    bool HasCheckInSelfie,
    bool HasCheckOutSelfie,
    bool? WithinFence,
    double? DistanceMeters,
    string? MatchedLocationName,
    string? Address,
    double? Latitude,
    double? Longitude,
    string? DeviceType,
    string? Browser,
    string? Os,
    bool? IsProxyVpn,
    string? NetworkNote,
    string? IpAddress,
    // Approval
    string ApprovalStatus,
    string? ApprovedByName,
    System.DateTimeOffset? ApprovedAt,
    string? RejectionReason,
    IReadOnlyList<AttendanceFlag> Flags);

public sealed record TeamAttendanceSummaryDto(
    int TeamSize,
    int PresentToday,
    int PendingApprovals,
    int FlaggedToday);

public sealed record TeamAttendanceDto(
    int SupervisorEmployeeId,
    bool SeesAll,
    TeamAttendanceSummaryDto Summary,
    IReadOnlyList<TeamAttendanceRowDto> Rows);

// ── Attendance correction requests ──

public sealed record AttendanceRequestDto(
    long Id,
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    System.DateOnly RequestDate,
    string RequestType,
    string? RequestedCheckInTime,
    string? RequestedCheckOutTime,
    string? RequestedStatus,
    string Reason,
    string Status,
    string? ReviewedByName,
    System.DateTimeOffset? ReviewedAt,
    string? ReviewNote,
    System.DateTimeOffset CreatedAt);
