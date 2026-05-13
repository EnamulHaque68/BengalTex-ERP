using System.Security.Cryptography;
using System.Text;
using BengalTex.ERP.Application.Auth;
using BengalTex.ERP.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace BengalTex.ERP.Infrastructure.Services;

/// <summary>
/// Pure auth concerns: credential validation, user info, refresh-token validation,
/// password change. Session lifecycle (start/end) is in SessionEnforcementService.
/// </summary>
public class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<ApplicationRole> roleManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
    }

    public async Task<CredentialValidationResult> ValidateCredentialsAsync(
        string emailOrUsername, string password, CancellationToken ct = default)
    {
        // Support both email and username login
        var user = emailOrUsername.Contains('@')
            ? await _userManager.FindByEmailAsync(emailOrUsername)
            : await _userManager.FindByNameAsync(emailOrUsername);

        if (user is null)
            return new CredentialValidationResult(false, false, false, null);

        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

        if (signInResult.IsLockedOut)
            return new CredentialValidationResult(false, true, false, null);

        if (signInResult.IsNotAllowed)
            return new CredentialValidationResult(false, false, true, null);

        if (!signInResult.Succeeded)
            return new CredentialValidationResult(false, false, false, null);

        var userAuthInfo = await BuildUserAuthInfoAsync(user);
        return new CredentialValidationResult(true, false, false, userAuthInfo);
    }

    public async Task<UserAuthInfo?> GetUserAuthInfoAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : await BuildUserAuthInfoAsync(user);
    }

    public async Task<UserAuthInfo?> ValidateRefreshTokenAsync(
        Guid userId, string rawRefreshToken, string sessionId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return null;

        // Session-id match: an active session was superseded by another login if these diverge.
        if (user.CurrentSessionId != sessionId) return null;

        // Refresh token not expired
        if (user.RefreshTokenExpiresAt <= DateTimeOffset.UtcNow) return null;

        // Validate refresh token hash using fixed-time comparison (timing-attack safe)
        if (string.IsNullOrEmpty(user.CurrentRefreshTokenHash)) return null;
        var incomingHash = HashToken(rawRefreshToken);
        var isValid = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(incomingHash),
            Encoding.UTF8.GetBytes(user.CurrentRefreshTokenHash));

        if (!isValid) return null;

        return await BuildUserAuthInfoAsync(user);
    }

    public async Task<(bool Succeeded, IEnumerable<string> Errors)> ChangePasswordAsync(
        Guid userId, string currentPassword, string newPassword, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return (false, new[] { "User not found." });

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        return result.Succeeded
            ? (true, Enumerable.Empty<string>())
            : (false, result.Errors.Select(e => e.Description));
    }

    public async Task<PasswordResetTokenResult?> GeneratePasswordResetTokenAsync(
        string email, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null || !user.IsActive) return null;

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        return new PasswordResetTokenResult(token, user.Email!, user.FullName);
    }

    public async Task<(bool Succeeded, IEnumerable<string> Errors)> ResetPasswordAsync(
        string email, string token, string newPassword, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        // Generic message — never leak whether email exists or token was the issue
        if (user is null)
            return (false, new[] { "Invalid email or reset token." });

        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        return result.Succeeded
            ? (true, Enumerable.Empty<string>())
            : (false, result.Errors.Select(e => e.Description));
    }

    private async Task<UserAuthInfo> BuildUserAuthInfoAsync(ApplicationUser user)
    {
        var roles = (await _userManager.GetRolesAsync(user)).ToList();
        var permissions = new HashSet<string>();

        foreach (var roleName in roles)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role is null) continue;
            var claims = await _roleManager.GetClaimsAsync(role);
            foreach (var claim in claims.Where(c => c.Type == "permission"))
                permissions.Add(claim.Value);
        }

        return new UserAuthInfo(
            user.Id,
            user.UserName!,
            user.Email!,
            user.FullName,
            user.FactoryId,
            user.IsActive,
            user.BoundDeviceFingerprint,
            roles.AsReadOnly(),
            permissions.ToList().AsReadOnly());
    }

    private static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hash);
    }
}
