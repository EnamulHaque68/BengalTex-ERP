using System.Security.Claims;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Role.Dtos;
using BengalTex.ERP.Infrastructure.Identity;
using BengalTex.ERP.Shared.Permissions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Infrastructure.Services;

/// <summary>
/// Admin-side role management. System roles (SuperAdmin, Admin) are protected
/// from rename/delete. Roles with members can't be deleted (would orphan
/// the user-role mapping).
///
/// Permission claim assignment lives in a separate service (2C).
/// </summary>
public class RoleManagementService : IRoleManagementService
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public RoleManagementService(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task<IReadOnlyList<RoleListItemDto>> ListRolesAsync(CancellationToken ct = default)
    {
        var roles = await _roleManager.Roles
            .OrderByDescending(r => r.IsSystemRole)
            .ThenBy(r => r.Name)
            .ToListAsync(ct);

        var items = new List<RoleListItemDto>(roles.Count);
        foreach (var role in roles)
        {
            var memberCount = (await _userManager.GetUsersInRoleAsync(role.Name!)).Count;
            var permissionCount = (await _roleManager.GetClaimsAsync(role))
                .Count(c => c.Type == "permission");

            items.Add(new RoleListItemDto(
                role.Id,
                role.Name ?? string.Empty,
                role.Description,
                role.IsSystemRole,
                memberCount,
                permissionCount));
        }

        return items;
    }

    public async Task<RoleDto?> GetRoleByIdAsync(Guid roleId, CancellationToken ct = default)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role is null) return null;

        var memberCount = (await _userManager.GetUsersInRoleAsync(role.Name!)).Count;
        var permissions = (await _roleManager.GetClaimsAsync(role))
            .Where(c => c.Type == "permission")
            .Select(c => c.Value)
            .OrderBy(p => p)
            .ToList();

        return new RoleDto(
            role.Id,
            role.Name ?? string.Empty,
            role.Description,
            role.IsSystemRole,
            memberCount,
            permissions.AsReadOnly());
    }

    public async Task<RoleCreateResult> CreateRoleAsync(CreateRoleData data, CancellationToken ct = default)
    {
        if (await _roleManager.RoleExistsAsync(data.Name))
            return new RoleCreateResult(false, null, new[] { $"Role '{data.Name}' already exists." });

        var role = new ApplicationRole
        {
            Name = data.Name,
            Description = data.Description,
            IsSystemRole = false   // System roles are seeded only, never created via API
        };

        var result = await _roleManager.CreateAsync(role);
        return result.Succeeded
            ? new RoleCreateResult(true, role.Id, Array.Empty<string>())
            : new RoleCreateResult(false, null, result.Errors.Select(e => e.Description).ToList());
    }

    public async Task<RoleOperationResult> UpdateRoleAsync(Guid roleId, UpdateRoleData data, CancellationToken ct = default)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role is null)
            return new RoleOperationResult(false, new[] { "Role not found." });

        if (role.IsSystemRole)
            return new RoleOperationResult(false, new[] { "System roles cannot be modified." });

        // Block rename to an existing role name
        if (!string.Equals(role.Name, data.Name, StringComparison.OrdinalIgnoreCase) &&
            await _roleManager.RoleExistsAsync(data.Name))
        {
            return new RoleOperationResult(false, new[] { $"Role '{data.Name}' already exists." });
        }

        role.Name = data.Name;
        role.Description = data.Description;

        var result = await _roleManager.UpdateAsync(role);
        return result.Succeeded
            ? new RoleOperationResult(true, Array.Empty<string>())
            : new RoleOperationResult(false, result.Errors.Select(e => e.Description).ToList());
    }

    public async Task<RoleOperationResult> DeleteRoleAsync(Guid roleId, CancellationToken ct = default)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role is null)
            return new RoleOperationResult(false, new[] { "Role not found." });

        if (role.IsSystemRole)
            return new RoleOperationResult(false, new[] { "System roles cannot be deleted." });

        var members = await _userManager.GetUsersInRoleAsync(role.Name!);
        if (members.Count > 0)
        {
            return new RoleOperationResult(false,
                new[] { $"Role has {members.Count} member(s). Unassign them first." });
        }

        var result = await _roleManager.DeleteAsync(role);
        return result.Succeeded
            ? new RoleOperationResult(true, Array.Empty<string>())
            : new RoleOperationResult(false, result.Errors.Select(e => e.Description).ToList());
    }

    public async Task<RoleOperationResult> UpdateRolePermissionsAsync(
        Guid roleId, IEnumerable<string> permissionKeys, CancellationToken ct = default)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role is null)
            return new RoleOperationResult(false, new[] { "Role not found." });

        // Validate every requested permission against the hardcoded catalog.
        // Permissions live in code, not DB — anything else is a bad client.
        var allKnown = Permissions.GetAll().ToHashSet(StringComparer.Ordinal);
        var requested = permissionKeys.Distinct(StringComparer.Ordinal).ToList();
        var unknown = requested.Where(p => !allKnown.Contains(p)).ToList();
        if (unknown.Count > 0)
            return new RoleOperationResult(false,
                new[] { $"Unknown permission(s): {string.Join(", ", unknown)}." });

        var existingClaims = await _roleManager.GetClaimsAsync(role);
        var existingPerms = existingClaims
            .Where(c => c.Type == "permission")
            .Select(c => c.Value)
            .ToHashSet(StringComparer.Ordinal);

        var toAdd = requested.Where(p => !existingPerms.Contains(p)).ToList();
        var toRemove = existingClaims
            .Where(c => c.Type == "permission" && !requested.Contains(c.Value))
            .ToList();

        foreach (var claim in toRemove)
        {
            var rm = await _roleManager.RemoveClaimAsync(role, claim);
            if (!rm.Succeeded)
                return new RoleOperationResult(false, rm.Errors.Select(e => e.Description).ToList());
        }

        foreach (var permission in toAdd)
        {
            var add = await _roleManager.AddClaimAsync(role, new Claim("permission", permission));
            if (!add.Succeeded)
                return new RoleOperationResult(false, add.Errors.Select(e => e.Description).ToList());
        }

        return new RoleOperationResult(true, Array.Empty<string>());
    }
}
