using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Infrastructure.Identity;

public class DeviceChangeRequest : BaseTransactionalEntity
{
    public Guid UserId { get; set; }
    public string OldDeviceFingerprint { get; set; } = string.Empty;
    public string NewDeviceFingerprint { get; set; } = string.Empty;
    public string? NewDeviceName { get; set; }
    public string? NewUserAgent { get; set; }
    public string? NewIpAddress { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DeviceChangeStatus Status { get; set; } = DeviceChangeStatus.Pending;
    public string? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public string? ReviewComment { get; set; }
}

public enum DeviceChangeStatus { Pending, Approved, Rejected, Cancelled }