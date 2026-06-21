using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>An academic qualification on an employee's profile (Education tab).</summary>
public class EmployeeEducation : BaseEntity
{
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public string Degree { get; set; } = string.Empty;          // e.g. "BSc in Textile Engineering"
    public string? Institute { get; set; }
    public int? PassingYear { get; set; }
    public string? Result { get; set; }                          // e.g. "CGPA 3.50" / "1st Division"

    public int SortOrder { get; set; }
}
