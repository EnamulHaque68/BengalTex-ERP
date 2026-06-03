namespace BengalTex.ERP.Application.Payroll.Dtos;

public sealed record FinalSettlementDto(
    long Id,
    string Code,
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    DateOnly SettlementDate,
    DateOnly LastWorkingDate,
    DateOnly JoiningDate,
    decimal YearsOfService,
    string Reason,
    decimal BasicSalary,
    decimal ProratedDays,
    decimal ProratedSalary,
    decimal LeaveEncashmentDays,
    decimal LeaveEncashmentAmount,
    decimal GratuityAmount,
    decimal OtherEarnings,
    decimal OutstandingLoan,
    decimal OtherDeductions,
    decimal GrossPayable,
    decimal TotalDeductions,
    decimal NetPayable,
    string Status,
    DateTimeOffset? ApprovedAt,
    string? ApprovedByUser,
    DateTimeOffset? PaidAt,
    string? PaymentMethod,
    string? PaymentReference,
    string? Notes);

/// <summary>
/// Read-only preview returned by the Calculate endpoint — auto-fills the create form.
/// </summary>
public sealed record FinalSettlementPreviewDto(
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    DateOnly JoiningDate,
    decimal BasicSalary,
    DateOnly LastWorkingDate,
    decimal YearsOfService,
    decimal ProratedDays,
    decimal ProratedSalary,
    decimal LeaveEncashmentDays,
    decimal LeaveEncashmentAmount,
    decimal GratuityAmount,
    decimal OutstandingLoan,
    decimal GrossPayable,
    decimal TotalDeductions,
    decimal NetPayable);

public sealed record BankAdviceRowDto(
    string EmployeeCode,
    string EmployeeName,
    string? BankName,
    string? BranchName,
    string? AccountNumber,
    string? RoutingNumber,
    decimal NetPay);
