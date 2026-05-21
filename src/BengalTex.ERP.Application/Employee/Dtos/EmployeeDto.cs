namespace BengalTex.ERP.Application.Employee.Dtos;

/// <summary>Full employee details for view/edit screens.</summary>
public record EmployeeDto(
    int Id,
    string Code,
    string FullName,
    string? Designation,
    string? Department,
    string? Phone,
    string? Email,
    string? NationalId,
    string? Address,
    DateOnly JoiningDate,
    DateOnly? DateOfBirth,
    string Gender,                 // enum as string
    string EmploymentType,         // enum as string
    decimal BasicSalary,
    string Status,                 // enum as string
    string? Notes,
    bool IsActive);

/// <summary>Compact employee for list/table views.</summary>
public record EmployeeListItemDto(
    int Id,
    string Code,
    string FullName,
    string? Designation,
    string? Department,
    string? Phone,
    string EmploymentType,
    decimal BasicSalary,
    string Status,
    bool IsActive);
