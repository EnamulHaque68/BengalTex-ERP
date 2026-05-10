using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Infrastructure.Identity;

public class UserDeviceHistory : BaseTransactionalEntity
{
    public Guid UserId { get; set; }
    public string DeviceFingerprint { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    public string? OperatingSystem { get; set; }
    public string? BrowserInfo { get; set; }
    public DeviceBindingStatus Status { get; set; }
    public DateTimeOffset BoundAt { get; set; }
    public DateTimeOffset? UnboundAt { get; set; }
    public string? UnbindReason { get; set; }
    public string? UnboundBy { get; set; }
}

public enum DeviceBindingStatus { Active, Replaced, Unbound, ForceUnbound }