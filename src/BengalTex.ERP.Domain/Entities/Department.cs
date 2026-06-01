using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Organizational department master (Production, Cutting, Sewing, Finishing, Admin, HR, …).
/// Supports a self-referencing parent for hierarchy (Production → Sewing Line 1). Employees
/// link via the optional <c>DepartmentId</c> FK; legacy free-text <see cref="Employee.Department"/>
/// remains for backward compatibility.
/// </summary>
public class Department : BaseEntity
{
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;

    public int? ParentDepartmentId { get; set; }
    public Department? ParentDepartment { get; set; }

    public int? HeadEmployeeId { get; set; }
    public Employee? HeadEmployee { get; set; }

    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
