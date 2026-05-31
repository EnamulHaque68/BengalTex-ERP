using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Production-floor machine master (sewing, cutting, embroidery, packaging …).
/// Referenced by <see cref="JobCard"/> for capacity planning + operator assignment.
/// </summary>
public class Machine : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    /// <summary>e.g. "Sewing", "Cutting", "Embroidery", "Packaging", "Boiler" — free text.</summary>
    public string? MachineType { get; set; }

    /// <summary>e.g. "Floor 2 / Line 3", "Cutting Room A" — free text.</summary>
    public string? Location { get; set; }

    /// <summary>Optional rated capacity per hour (units/hr), used for future planning.</summary>
    public decimal? CapacityPerHour { get; set; }

    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}
