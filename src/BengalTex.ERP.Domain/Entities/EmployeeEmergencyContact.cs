using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>An emergency contact on an employee's profile (Emergency Contact tab).</summary>
public class EmployeeEmergencyContact : BaseEntity
{
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Relationship { get; set; }                    // e.g. "Father", "Spouse"
    public string Phone { get; set; } = string.Empty;
    public string? Address { get; set; }

    public int SortOrder { get; set; }
}
