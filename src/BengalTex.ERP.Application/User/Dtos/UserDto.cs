namespace BengalTex.ERP.Application.User.Dtos;

/// <summary>
/// Full user details for view/edit screens.
/// </summary>
public record UserDto(
    Guid Id,
    string UserName,
    string Email,
    string FullName,
    int? FactoryId,
    bool IsActive,
    bool EmailConfirmed,
    bool IsLockedOut,
    DateTimeOffset? LockoutEnd,
    int AccessFailedCount,
    string? BoundDeviceFingerprint,
    string? BoundDeviceName,
    DateTimeOffset? DeviceBoundAt,
    DateTimeOffset CreatedAt,
    string? CreatedBy,
    IReadOnlyList<string> Roles);
