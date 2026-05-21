using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Employee master (HR foundation — Attendance + Payroll build on this).
/// System-wide unique code (auto-generated via NumberingService "EMP" when not supplied).
/// <see cref="BasicSalary"/> is the monthly basic in base currency (BDT) — used by payroll.
/// </summary>
public class Employee : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    public string? Designation { get; set; }
    public string? Department { get; set; }

    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? NationalId { get; set; }    // Bangladesh NID
    public string? Address { get; set; }

    public DateOnly JoiningDate { get; set; }
    public DateOnly? DateOfBirth { get; set; }

    public Gender Gender { get; set; } = Gender.Male;
    public EmploymentType EmploymentType { get; set; } = EmploymentType.Permanent;

    /// <summary>Monthly basic salary in base currency (BDT). 0 for daily-wage workers paid per attendance.</summary>
    public decimal BasicSalary { get; set; }

    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;

    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public enum Gender
{
    Male = 1,
    Female = 2,
    Other = 3
}

public enum EmploymentType
{
    Permanent = 1,
    Contract = 2,
    DailyWage = 3
}

public enum EmployeeStatus
{
    Active = 1,
    Inactive = 2,
    Terminated = 3
}
