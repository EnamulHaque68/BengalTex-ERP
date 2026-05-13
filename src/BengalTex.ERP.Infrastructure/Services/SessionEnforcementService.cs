using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace BengalTex.ERP.Infrastructure.Services;

/// <summary>
/// Persists single-session-per-user enforcement state on ApplicationUser.
/// Session state (CurrentSessionId, CurrentRefreshTokenHash, RefreshTokenExpiresAt)
/// lives on the user row; rotating these fields on a new login automatically
/// invalidates any previously-issued refresh token.
///
/// On replacement, broadcasts "SessionSuperseded" via ISessionBroadcaster so any
/// open tabs/devices on the prior session can self-terminate immediately.
/// </summary>
public class SessionEnforcementService : ISessionEnforcementService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISessionBroadcaster _broadcaster;

    public SessionEnforcementService(
        UserManager<ApplicationUser> userManager,
        ISessionBroadcaster broadcaster)
    {
        _userManager = userManager;
        _broadcaster = broadcaster;
    }

    public async Task EnforceSingleSessionAsync(
        Guid userId, string newSessionId, string newRefreshTokenHash,
        DateTimeOffset expiresAt, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return;

        var previousSessionId = user.CurrentSessionId;

        // Overwriting these three fields supersedes any prior session:
        // the old refresh token hash no longer matches, so refresh attempts will fail.
        user.CurrentSessionId = newSessionId;
        user.CurrentRefreshTokenHash = newRefreshTokenHash;
        user.RefreshTokenExpiresAt = expiresAt;

        await _userManager.UpdateAsync(user);

        // Only broadcast if a different prior session actually existed
        if (!string.IsNullOrEmpty(previousSessionId) && previousSessionId != newSessionId)
        {
            await _broadcaster.NotifySessionSupersededAsync(userId, newSessionId, ct);
        }
    }

    public async Task<bool> IsCurrentSessionAsync(Guid userId, string sessionId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return false;

        return user.CurrentSessionId == sessionId
               && user.RefreshTokenExpiresAt is not null
               && user.RefreshTokenExpiresAt > DateTimeOffset.UtcNow;
    }

    public async Task ClearSessionAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return;

        user.CurrentSessionId = null;
        user.CurrentRefreshTokenHash = null;
        user.RefreshTokenExpiresAt = null;

        await _userManager.UpdateAsync(user);
    }
}
