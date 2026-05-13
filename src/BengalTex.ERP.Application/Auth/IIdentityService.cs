namespace BengalTex.ERP.Application.Auth;

/// <summary>
/// Owns auth concerns only: credential validation, user info lookup,
/// refresh-token validation, password change/reset.
/// Session lifecycle (start/end/check) is handled by ISessionEnforcementService.
/// </summary>
public interface IIdentityService
{
    Task<CredentialValidationResult> ValidateCredentialsAsync(
        string emailOrUsername, string password, CancellationToken ct = default);

    Task<UserAuthInfo?> GetUserAuthInfoAsync(Guid userId, CancellationToken ct = default);

    Task<UserAuthInfo?> ValidateRefreshTokenAsync(
        Guid userId, string rawRefreshToken, string sessionId, CancellationToken ct = default);

    Task<(bool Succeeded, IEnumerable<string> Errors)> ChangePasswordAsync(
        Guid userId, string currentPassword, string newPassword, CancellationToken ct = default);

    /// <summary>
    /// Generates a password-reset token for the user identified by email.
    /// Returns null if no active user matches — caller should still respond with
    /// a generic success message to avoid user enumeration.
    /// </summary>
    Task<PasswordResetTokenResult?> GeneratePasswordResetTokenAsync(
        string email, CancellationToken ct = default);

    /// <summary>
    /// Validates the reset token (issued by GeneratePasswordResetTokenAsync) and
    /// updates the user's password if the token is valid.
    /// </summary>
    Task<(bool Succeeded, IEnumerable<string> Errors)> ResetPasswordAsync(
        string email, string token, string newPassword, CancellationToken ct = default);
}

public record CredentialValidationResult(
    bool Succeeded,
    bool IsLockedOut,
    bool IsNotAllowed,
    UserAuthInfo? User);

public record UserAuthInfo(
    Guid UserId,
    string UserName,
    string Email,
    string FullName,
    int? FactoryId,
    bool IsActive,
    string? BoundDeviceFingerprint,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

public record PasswordResetTokenResult(
    string Token,
    string Email,
    string FullName);
