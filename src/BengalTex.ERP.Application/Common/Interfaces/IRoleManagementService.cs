using BengalTex.ERP.Application.Role.Dtos;

namespace BengalTex.ERP.Application.Common.Interfaces;

/// <summary>
/// Admin-side role management (separate from permission assignment which is 2C).
/// Implementation lives in Infrastructure (uses RoleManager + UserManager).
/// ApplicationRole is an Infrastructure-layer Identity entity — this service
/// returns DTOs / data records so it doesn't leak into the Application layer.
/// </summary>
public interface IRoleManagementService
{
    Task<IReadOnlyList<RoleListItemDto>> ListRolesAsync(CancellationToken ct = default);

    Task<RoleDto?> GetRoleByIdAsync(Guid roleId, CancellationToken ct = default);

    Task<RoleCreateResult> CreateRoleAsync(
        CreateRoleData data, CancellationToken ct = default);

    Task<RoleOperationResult> UpdateRoleAsync(
        Guid roleId, UpdateRoleData data, CancellationToken ct = default);

    Task<RoleOperationResult> DeleteRoleAsync(Guid roleId, CancellationToken ct = default);

    /// <summary>
    /// Replaces a role's permission set (diff-add and diff-remove against current).
    /// Unknown permission keys are rejected. System roles like SuperAdmin are intentionally
    /// NOT blocked — admins may legitimately need to grant/revoke perms on system roles.
    /// </summary>
    Task<RoleOperationResult> UpdateRolePermissionsAsync(
        Guid roleId, IEnumerable<string> permissionKeys, CancellationToken ct = default);
}

public record CreateRoleData(string Name, string? Description);

public record UpdateRoleData(string Name, string? Description);

public record RoleCreateResult(
    bool Succeeded,
    Guid? RoleId,
    IReadOnlyList<string> Errors);

public record RoleOperationResult(
    bool Succeeded,
    IReadOnlyList<string> Errors);
