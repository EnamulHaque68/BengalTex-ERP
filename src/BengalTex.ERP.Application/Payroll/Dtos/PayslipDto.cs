namespace BengalTex.ERP.Application.Payroll.Dtos;

public sealed record PayslipDto(
    long Id,
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    int Year,
    int Month,
    decimal BasicSalary,
    decimal PresentDays,
    decimal AbsentDays,
    decimal LeaveDays,
    decimal OvertimeHours,
    decimal OvertimeAmount,
    decimal Allowances,
    decimal Deductions,
    decimal GrossPay,
    decimal NetPay,
    string Status,                 // Draft | Paid
    DateTimeOffset? PaidAt,
    string? Notes);
