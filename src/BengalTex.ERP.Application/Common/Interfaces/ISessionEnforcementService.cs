namespace BengalTex.ERP.Application.Common.Interfaces;

/// <summary>
/// Owns the user session lifecycle: start (with single-session enforcement),
/// validate, and end. Kept separate from IIdentityService so that auth concerns
/// (credentials, password, user info) stay decoupled from session storage.
/// </summary>
public interface ISessionEnforcementService
{
    /// <summary>
    /// Enforces single-active-session per user.
    /// Called on successful login: invalidates any existing refresh token,
    /// stores the new one, broadcasts session-terminated to old session via SignalR.
    /// </summary>
    Task EnforceSingleSessionAsync(Guid userId, string newSessionId, string newRefreshTokenHash,
        DateTimeOffset expiresAt, CancellationToken ct = default);

    /// <summary>
    /// Validates that the current refresh token matches the one stored.
    /// If not, the session has been superseded.
    /// </summary>
    Task<bool> IsCurrentSessionAsync(Guid userId, string sessionId, CancellationToken ct = default);

    /// <summary>
    /// Ends the current session (logout). Clears stored refresh token + session id.
    /// </summary>
    Task ClearSessionAsync(Guid userId, CancellationToken ct = default);
}
