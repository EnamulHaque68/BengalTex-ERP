namespace BengalTex.ERP.Application.Payroll.Dtos;

/// <summary>
/// Enriched payslip data for the printable salary-slip view — joins Employee + Department +
/// Designation + BankAccount onto the payslip's own fields. Distinct from <see cref="PayslipDto"/>
/// (used in list/edit dialogs) to avoid bloating that DTO with rarely-needed fields.
/// </summary>
public sealed record PayslipPrintDto(
    long Id,
    string PayslipCode,             // synthesized for display: e.g. "PS-2026-06-EMP-0001"

    // ── Employee ──
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string? Designation,
    string? Department,
    string? EmployeePhone,
    string? EmployeeNationalId,
    DateOnly? JoiningDate,
    string? EmploymentType,

    // ── Period ──
    int Year,
    int Month,
    string MonthName,               // "June"

    // ── Attendance ──
    decimal PresentDays,
    decimal AbsentDays,
    decimal LeaveDays,
    decimal OvertimeHours,

    // ── Earnings ──
    decimal BasicSalary,
    decimal HouseRent,
    decimal Medical,
    decimal Transport,
    decimal FoodAllowance,
    decimal FestivalBonus,
    decimal Allowances,
    decimal OvertimeAmount,
    decimal GrossPay,

    // ── Deductions ──
    decimal PfEmployee,
    decimal PfEmployer,             // info-only — paid by employer, not deducted
    decimal IncomeTax,
    decimal LoanDeduction,
    decimal OtherDeductions,        // absence/etc — Deductions − PF − Tax − Loan
    decimal TotalDeductions,

    // ── Net ──
    decimal NetPay,

    string Status,
    DateTimeOffset? PaidAt,
    string? Notes,

    // ── Bank account (where the salary is paid) ──
    string? BankName,
    string? BankBranch,
    string? BankAccountNumber,

    // ── Company (header / footer) ──
    string CompanyName,
    string? CompanyShortName,
    string? CompanyAddressLine1,
    string? CompanyAddressLine2,
    string? CompanyCity,
    string? CompanyDistrict,
    string? CompanyPostalCode,
    string? CompanyPhone,
    string? CompanyEmail,
    string? CompanyTaxNumber,
    string? CompanyLogoUrl);
