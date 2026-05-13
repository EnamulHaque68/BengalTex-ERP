using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

public class Factory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string? District { get; set; }
    public string? PostalCode { get; set; }
    public string? Phone { get; set; }

    public bool IsActive { get; set; } = true;

    // Geo-fence for attendance check-in validation
    public double? GeoFenceLat { get; set; }
    public double? GeoFenceLng { get; set; }
    public double? GeoFenceRadiusMeters { get; set; }

    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
}
