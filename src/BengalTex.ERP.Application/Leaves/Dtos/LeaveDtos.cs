namespace BengalTex.ERP.Application.Leaves.Dtos;

public record LeaveTypeDto(int Id, string Code, string Name, bool IsPaid,
    decimal AnnualEntitlement, int? MaxConsecutiveDays, string? Description, bool IsActive);

public record HolidayDto(int Id, DateOnly Date, string Name, string? Description, bool IsActive);

public record LeaveBalanceDto(
    int Id, int EmployeeId, string EmployeeCode, string EmployeeName,
    int LeaveTypeId, string LeaveTypeCode, string LeaveTypeName,
    int Year, decimal Entitled, decimal Taken, decimal Remaining);

public record LeaveApplicationDto(
    long Id, string Code,
    int EmployeeId, string EmployeeCode, string EmployeeName,
    int LeaveTypeId, string LeaveTypeCode, string LeaveTypeName,
    DateOnly FromDate, DateOnly ToDate, decimal TotalDays,
    string? Reason, string Status,
    DateTimeOffset? DecidedAt, string? DecidedBy, string? RejectionReason,
    bool WriteAttendance, string? Notes);

public record LeaveApplicationListItemDto(
    long Id, string Code,
    int EmployeeId, string EmployeeCode, string EmployeeName,
    string LeaveTypeCode, string LeaveTypeName,
    DateOnly FromDate, DateOnly ToDate, decimal TotalDays,
    string Status, string? Reason);
