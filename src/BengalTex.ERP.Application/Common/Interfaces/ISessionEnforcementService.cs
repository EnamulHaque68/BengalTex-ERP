namespace BengalTex.ERP.Application.Common.Interfaces;

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
}