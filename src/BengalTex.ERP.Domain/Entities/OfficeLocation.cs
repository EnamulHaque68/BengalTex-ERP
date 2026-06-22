using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// A geo-fenced location a company operates from (head office, factory, warehouse, branch).
/// Generalises the single per-factory geo-fence into a multi-location model: an employee is
/// authorized for one or more locations (<see cref="EmployeeOfficeLocation"/>) and a self
/// check-in is valid if it falls inside ANY of their authorized locations' radius.
/// </summary>
public class OfficeLocation : BaseEntity
{
    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public OfficeLocationType Type { get; set; } = OfficeLocationType.HeadOffice;

    public double Latitude { get; set; }
    public double Longitude { get; set; }
    /// <summary>Allowed check-in radius in metres (default 10).</summary>
    public double RadiusMeters { get; set; } = 10;

    public string? Address { get; set; }

    public bool IsActive { get; set; } = true;
}

public enum OfficeLocationType
{
    HeadOffice = 1,
    Factory = 2,
    Warehouse = 3,
    BranchOffice = 4
}

/// <summary>Join: which office locations an employee is authorized to attend from.</summary>
public class EmployeeOfficeLocation : BaseEntity
{
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public int OfficeLocationId { get; set; }
    public OfficeLocation OfficeLocation { get; set; } = null!;
}
