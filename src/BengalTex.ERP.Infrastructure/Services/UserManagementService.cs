using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.User.Dtos;
using BengalTex.ERP.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Infrastructure.Services;

/// <summary>
/// Admin-side user management — backed by ASP.NET Core Identity.
/// All operations return DTOs / data records so ApplicationUser never leaks
/// into the Application layer.
/// </summary>
public class UserManagementService : IUserManagementService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public UserManagementService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<PagedResult<UserListItemDto>> ListUsersAsync(
        PagedQueryParameters parameters, CancellationToken ct = default)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = parameters.Search.Trim();
            query = query.Where(u =>
                u.UserName!.Contains(search) ||
                u.Email!.Contains(search) ||
                u.FullName.Contains(search));
        }

        query = (parameters.SortBy?.ToLowerInvariant(), parameters.SortDirection?.ToLowerInvariant()) switch
        {
            ("username", "desc") => query.OrderByDescending(u => u.UserName),
            ("email", "desc")    => query.OrderByDescending(u => u.Email),
            ("email", _)         => query.OrderBy(u => u.Email),
            ("fullname", "desc") => query.OrderByDescending(u => u.FullName),
            ("fullname", _)      => query.OrderBy(u => u.FullName),
            ("createdat", "desc") => query.OrderByDescending(u => u.CreatedAt),
            ("createdat", _)     => query.OrderBy(u => u.CreatedAt),
            _                    => query.OrderBy(u => u.UserName)
        };

        var totalCount = await query.CountAsync(ct);
        var users = await query
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(ct);

        var items = new List<UserListItemDto>(users.Count);
        var now = DateTimeOffset.UtcNow;
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            items.Add(new UserListItemDto(
                user.Id,
                user.UserName ?? string.Empty,
                user.Email ?? string.Empty,
                user.FullName,
                user.FactoryId,
                user.IsActive,
                IsLockedOut: user.LockoutEnd is not null && user.LockoutEnd > now,
                roles.ToList().AsReadOnly(),
                user.CreatedAt));
        }

        return PagedResult<UserListItemDto>.Create(items, parameters.Page, parameters.PageSize, totalCount);
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        var now = DateTimeOffset.UtcNow;

        return new UserDto(
            user.Id,
            user.UserName ?? string.Empty,
            user.Email ?? string.Empty,
            user.FullName,
            user.FactoryId,
            user.IsActive,
            user.EmailConfirmed,
            IsLockedOut: user.LockoutEnd is not null && user.LockoutEnd > now,
            user.LockoutEnd,
            user.AccessFailedCount,
            user.BoundDeviceFingerprint,
            user.BoundDeviceName,
            user.DeviceBoundAt,
            user.CreatedAt,
            user.CreatedBy,
            roles.ToList().AsReadOnly());
    }

    public async Task<UserCreateResult> CreateUserAsync(CreateUserData data, CancellationToken ct = default)
    {
        // Validate roles exist before creating the user (avoid partial-success states).
        var rolesToAssign = data.Roles.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var invalidRoles = new List<string>();
        foreach (var role in rolesToAssign)
        {
            if (!await _roleManager.RoleExistsAsync(role))
                invalidRoles.Add(role);
        }
        if (invalidRoles.Count > 0)
            return new UserCreateResult(false, null,
                new[] { $"Unknown role(s): {string.Join(", ", invalidRoles)}." });

        var user = new ApplicationUser
        {
            UserName = data.UserName,
            Email = data.Email,
            FullName = data.FullName,
            FactoryId = data.FactoryId,
            IsActive = true,
            EmailConfirmed = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "admin"
        };

        var createResult = await _userManager.CreateAsync(user, data.Password);
        if (!createResult.Succeeded)
            return new UserCreateResult(false, null,
                createResult.Errors.Select(e => e.Description).ToList());

        if (rolesToAssign.Count > 0)
        {
            var roleResult = await _userManager.AddToRolesAsync(user, rolesToAssign);
            if (!roleResult.Succeeded)
            {
                // Roll back the user we just created to avoid orphaned account without intended roles
                await _userManager.DeleteAsync(user);
                return new UserCreateResult(false, null,
                    roleResult.Errors.Select(e => e.Description).ToList());
            }
        }

        return new UserCreateResult(true, user.Id, Array.Empty<string>());
    }

    public async Task<UserOperationResult> UpdateUserAsync(Guid userId, UpdateUserData data, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return new UserOperationResult(false, new[] { "User not found." });

        user.UserName = data.UserName;
        user.NormalizedUserName = _userManager.NormalizeName(data.UserName);
        user.Email = data.Email;
        user.NormalizedEmail = _userManager.NormalizeEmail(data.Email);
        user.FullName = data.FullName;
        user.FactoryId = data.FactoryId;

        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded
            ? new UserOperationResult(true, Array.Empty<string>())
            : new UserOperationResult(false, result.Errors.Select(e => e.Description).ToList());
    }

    public async Task<UserOperationResult> SetUserActiveAsync(Guid userId, bool isActive, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return new UserOperationResult(false, new[] { "User not found." });

        if (user.IsActive == isActive)
            return new UserOperationResult(true, Array.Empty<string>());

        user.IsActive = isActive;

        // Deactivating also clears the bound device + session so a reactivated user must re-bind.
        if (!isActive)
        {
            user.CurrentSessionId = null;
            user.CurrentRefreshTokenHash = null;
            user.RefreshTokenExpiresAt = null;
        }

        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded
            ? new UserOperationResult(true, Array.Empty<string>())
            : new UserOperationResult(false, result.Errors.Select(e => e.Description).ToList());
    }

    public async Task<UserOperationResult> UpdateUserRolesAsync(
        Guid userId, IEnumerable<string> roleNames, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return new UserOperationResult(false, new[] { "User not found." });

        var targetRoles = roleNames.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        // Validate every target role exists
        var invalidRoles = new List<string>();
        foreach (var role in targetRoles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
                invalidRoles.Add(role);
        }
        if (invalidRoles.Count > 0)
            return new UserOperationResult(false,
                new[] { $"Unknown role(s): {string.Join(", ", invalidRoles)}." });

        var currentRoles = (await _userManager.GetRolesAsync(user)).ToList();

        var toRemove = currentRoles
            .Where(r => !targetRoles.Contains(r, StringComparer.OrdinalIgnoreCase))
            .ToList();
        var toAdd = targetRoles
            .Where(r => !currentRoles.Contains(r, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (toRemove.Count > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, toRemove);
            if (!removeResult.Succeeded)
                return new UserOperationResult(false,
                    removeResult.Errors.Select(e => e.Description).ToList());
        }

        if (toAdd.Count > 0)
        {
            var addResult = await _userManager.AddToRolesAsync(user, toAdd);
            if (!addResult.Succeeded)
                return new UserOperationResult(false,
                    addResult.Errors.Select(e => e.Description).ToList());
        }

        return new UserOperationResult(true, Array.Empty<string>());
    }

    public async Task<UserOperationResult> AdminResetPasswordAsync(
        Guid userId, string newPassword, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return new UserOperationResult(false, new[] { "User not found." });

        // Generate a reset token internally so we can use the same ResetPasswordAsync path
        // (also runs password validators and respects password history if configured).
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        return result.Succeeded
            ? new UserOperationResult(true, Array.Empty<string>())
            : new UserOperationResult(false, result.Errors.Select(e => e.Description).ToList());
    }
}
