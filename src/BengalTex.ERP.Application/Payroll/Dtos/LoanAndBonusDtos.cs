namespace BengalTex.ERP.Application.Payroll.Dtos;

public record EmployeeLoanDto(
    long Id,
    string Code,
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    DateOnly IssuedDate,
    decimal Principal,
    decimal EmiAmount,
    int TenureMonths,
    int StartYearMonth,
    decimal OutstandingPrincipal,
    decimal TotalRepaid,           // computed = Principal − Outstanding
    string Status,
    string? Notes);

public record FestivalBonusDto(
    long Id,
    string Code,
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    int BonusYear,
    string BonusType,
    decimal Amount,
    string Status,
    string PaymentMethod,
    DateTimeOffset? PaidAt,
    string? PaidBy,
    string? Notes);
