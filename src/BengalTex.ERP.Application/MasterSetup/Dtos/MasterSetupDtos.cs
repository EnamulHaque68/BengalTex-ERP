namespace BengalTex.ERP.Application.MasterSetup.Dtos;

public record DepartmentDto(
    int Id, string? Code, string Name,
    int? ParentDepartmentId, string? ParentDepartmentName,
    int? HeadEmployeeId, string? HeadEmployeeName,
    string? Description, bool IsActive);

public record DesignationDto(
    int Id, string? Code, string Name,
    int? GradeLevel, string? Description, bool IsActive);

public record ShiftDto(
    int Id, string Code, string Name,
    string StartTime,                  // "HH:mm" wire format
    string EndTime,
    string WeekendDayOfWeek,           // enum-as-string
    string? SecondWeekendDayOfWeek,
    string? Description, bool IsActive);

public record BankAccountDto(
    int Id, string AccountName, string BankName, string? BranchName,
    string AccountNumber, string AccountType,
    string? RoutingNumber, string? SwiftCode, string Currency,
    int? LedgerAccountId, string? LedgerAccountCode, string? LedgerAccountName,
    string? Notes, bool IsActive);
