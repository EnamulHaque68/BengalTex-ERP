using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.User.Dtos;

namespace BengalTex.ERP.Application.Common.Interfaces;

/// <summary>
/// Admin-side user operations: list/search, create, update, deactivate, role assignment,
/// admin password reset. Kept separate from IIdentityService which owns self-service
/// auth (login, change-own-password, refresh).
///
/// Implementation lives in Infrastructure (uses UserManager + RoleManager).
/// ApplicationUser is an Infrastructure-layer Identity entity, so this service returns
/// DTOs/data records and never leaks the entity type into the Application layer.
/// </summary>
public interface IUserManagementService
{
    Task<PagedResult<UserListItemDto>> ListUsersAsync(
        PagedQueryParameters parameters, CancellationToken ct = default);

    Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken ct = default);

    Task<UserCreateResult> CreateUserAsync(
        CreateUserData data, CancellationToken ct = default);

    Task<UserOperationResult> UpdateUserAsync(
        Guid userId, UpdateUserData data, CancellationToken ct = default);

    Task<UserOperationResult> SetUserActiveAsync(
        Guid userId, bool isActive, CancellationToken ct = default);

    Task<UserOperationResult> UpdateUserRolesAsync(
        Guid userId, IEnumerable<string> roleNames, CancellationToken ct = default);

    Task<UserOperationResult> AdminResetPasswordAsync(
        Guid userId, string newPassword, CancellationToken ct = default);
}

/// <summary>Input data for creating a user (Command → Service translation).</summary>
public record CreateUserData(
    string UserName,
    string Email,
    string FullName,
    string Password,
    int? FactoryId,
    IEnumerable<string> Roles);

/// <summary>Input data for updating a user's basic profile (no password, no roles).</summary>
public record UpdateUserData(
    string UserName,
    string Email,
    string FullName,
    int? FactoryId);

/// <summary>Result for CreateUser — exposes the new user id on success.</summary>
public record UserCreateResult(
    bool Succeeded,
    Guid? UserId,
    IReadOnlyList<string> Errors);

/// <summary>Generic result for non-create user operations.</summary>
public record UserOperationResult(
    bool Succeeded,
    IReadOnlyList<string> Errors);
