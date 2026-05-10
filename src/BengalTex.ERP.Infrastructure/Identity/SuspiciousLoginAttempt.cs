using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Infrastructure.Identity;

public class SuspiciousLoginAttempt : BaseTransactionalEntity
{
    public string AttemptedUserName { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string DeviceFingerprint { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    public string? OperatingSystem { get; set; }
    public double? Latitude { get; set; }            // GPS at attempt time (nullable)
    public double? Longitude { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool AdminNotified { get; set; }
    public bool EmployeeNotified { get; set; }
    public DateTimeOffset AttemptedAt { get; set; }
}