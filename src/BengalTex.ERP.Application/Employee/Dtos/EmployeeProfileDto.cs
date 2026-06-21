namespace BengalTex.ERP.Application.Employee.Dtos;

/// <summary>One leave-type balance line shown in the profile "Leave Balance" card.</summary>
public sealed record ProfileLeaveBalanceDto(string LeaveTypeName, decimal Entitled, decimal Taken, decimal Remaining);

/// <summary>One payslip in the profile "Latest Payslip" card.</summary>
public sealed record ProfilePayslipDto(long PayslipId, int Year, int Month, string MonthLabel, decimal NetPay, string Status);

/// <summary>Current-month attendance breakdown for the profile donut card.</summary>
public sealed record ProfileAttendanceSummaryDto(
    int Year, int Month, string MonthLabel,
    int PresentDays, int LateDays, int AbsentDays, int LeaveDays, int TotalWorkingDays);

/// <summary>A rated skill bar on the profile.</summary>
public sealed record ProfileSkillDto(int Id, string Name, int ProficiencyPercent);

/// <summary>An education record on the profile.</summary>
public sealed record ProfileEducationDto(int Id, string Degree, string? Institute, int? PassingYear, string? Result);

/// <summary>An emergency contact on the profile.</summary>
public sealed record ProfileEmergencyContactDto(int Id, string Name, string? Relationship, string Phone, string? Address);

/// <summary>
/// Aggregated, read-only view backing the Employee Profile page (header + overview).
/// Combines the employee master with their bank, line manager, leave balances and recent payslips.
/// </summary>
public sealed record EmployeeProfileDto(
    // Identity / header
    int Id,
    string Code,
    string FullName,
    string? Designation,
    string? Department,
    string? PhotoUrl,
    bool IsActive,
    string Status,
    DateOnly JoiningDate,
    string EmploymentType,
    string? WorkLocation,
    // Contact / personal
    string? Email,
    string? Phone,
    DateOnly? DateOfBirth,
    string? Nationality,
    string? BloodGroup,
    string Gender,
    string MaritalStatus,
    string? Religion,
    string? NationalId,
    string? Address,
    string? AboutMe,
    // Job summary
    int? ReportingToEmployeeId,
    string? ReportingToName,
    DateOnly? ProbationEndDate,
    DateOnly? ConfirmationDate,
    // Compensation & payroll
    decimal BasicSalary,
    decimal HouseRentAllowance,
    decimal MedicalAllowance,
    decimal TransportAllowance,
    decimal FoodAllowance,
    decimal GrossSalary,
    string? BankName,
    string? AccountNumberMasked,
    // Related collections
    IReadOnlyList<ProfileLeaveBalanceDto> LeaveBalances,
    IReadOnlyList<ProfilePayslipDto> LatestPayslips,
    IReadOnlyList<ProfileSkillDto> Skills,
    IReadOnlyList<ProfileEducationDto> Education,
    IReadOnlyList<ProfileEmergencyContactDto> EmergencyContacts,
    ProfileAttendanceSummaryDto Attendance,
    // Access / linkage
    string? UserId,
    bool CanEdit);
