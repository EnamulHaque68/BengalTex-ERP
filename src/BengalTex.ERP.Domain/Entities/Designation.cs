using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Job designation master (Sewing Operator, Cutter, Floor Supervisor, …).
/// <c>GradeLevel</c> 1-10 supports salary-grade hierarchy (used in future Salary Grade module).
/// Employees link via optional <c>DesignationId</c>; legacy free-text
/// <see cref="Employee.Designation"/> stays for backward compatibility.
/// </summary>
public class Designation : BaseEntity
{
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional hierarchy level 1-10 (1 = lowest, 10 = highest).</summary>
    public int? GradeLevel { get; set; }

    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
