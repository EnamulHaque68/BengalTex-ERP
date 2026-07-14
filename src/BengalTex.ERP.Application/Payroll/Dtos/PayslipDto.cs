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
    // BD payroll breakdown — earnings
    decimal HouseRent,
    decimal Medical,
    decimal Transport,
    decimal FoodAllowance,
    decimal FestivalBonus,
    // BD payroll breakdown — deductions
    decimal PfEmployee,
    decimal PfEmployer,
    decimal IncomeTax,
    decimal LoanDeduction,
    decimal GrossPay,
    decimal NetPay,
    string Status,                 // Draft | Approved | Paid
    DateTimeOffset? PaidAt,
    string? Notes);
