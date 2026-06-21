using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>A rated skill on an employee's profile (e.g. "Production Management" at 90%).</summary>
public class EmployeeSkill : BaseEntity
{
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    /// <summary>Proficiency 0–100 (rendered as a progress bar).</summary>
    public int ProficiencyPercent { get; set; }

    public int SortOrder { get; set; }
}
