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

    // Optional FKs to master tables (v1a: free-text columns above preserved for back-compat;
    // future v1b can backfill + drop free-text). Restrict on delete to protect employee data.
    public int? DepartmentId { get; set; }
    public Department? DepartmentEntity { get; set; }

    public int? DesignationId { get; set; }
    public Designation? DesignationEntity { get; set; }

    public int? ShiftId { get; set; }
    public Shift? Shift { get; set; }

    public int? BankAccountId { get; set; }
    public BankAccount? BankAccount { get; set; }

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

    // BD payroll allowance components (fixed monthly amounts in BDT, all default 0)
    public decimal HouseRentAllowance { get; set; }
    public decimal MedicalAllowance { get; set; }
    public decimal TransportAllowance { get; set; }
    public decimal FoodAllowance { get; set; }

    // Provident Fund: opt-in, contribution = Basic * PfRate% (employee + employer each)
    public bool IsPfMember { get; set; }
    public decimal PfRate { get; set; } = 10m;

    // Bangladesh income tax (slab-based on annualised gross). Opt-in per employee.
    public bool IsTaxable { get; set; }

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
