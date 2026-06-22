namespace BengalTex.ERP.Application.Employee.Dtos;

/// <summary>
/// The login-account state for an employee: whether a User is linked, its roles, and the access role
/// suggested by the employee's designation (so SuperAdmin can grant access straight from job designation).
/// </summary>
public sealed record EmployeeLoginStatusDto(
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    bool HasLogin,
    string? UserId,
    string? UserName,
    string? Email,
    bool? UserIsActive,
    IReadOnlyList<string> Roles,
    string? DesignationName,
    string? DesignationAccessRoleName,    // the role the designation grants (suggested)
    string SuggestedUserName,             // default username if creating (employee code)
    string? EmployeeEmail);               // the employee's own email — prefilled when creating a login
