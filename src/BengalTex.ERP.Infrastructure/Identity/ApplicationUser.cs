using Microsoft.AspNetCore.Identity;

namespace BengalTex.ERP.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public int? FactoryId { get; set; }                       // Primary factory assignment
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }

    // Device binding
    public string? BoundDeviceFingerprint { get; set; }
    public string? BoundDeviceName { get; set; }
    public DateTimeOffset? DeviceBoundAt { get; set; }

    // Refresh token (single active session enforcement)
    public string? CurrentRefreshTokenHash { get; set; }
    public string? CurrentSessionId { get; set; }
    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }

    public ICollection<UserDeviceHistory> DeviceHistory { get; set; } = new List<UserDeviceHistory>();
}

public class ApplicationRole : IdentityRole<Guid>
{
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }      // Cannot be deleted
}