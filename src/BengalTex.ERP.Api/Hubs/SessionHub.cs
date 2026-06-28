using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BengalTex.ERP.Api.Hubs;

/// <summary>
/// SignalR hub for session-related real-time events.
/// On connect, each authenticated socket joins a per-user group named "user-{userId}"
/// so the server can target all of a user's open tabs/devices at once.
///
/// Currently the server only emits "SessionSuperseded" (when the same user logs in
/// from another device, the older session is force-disconnected).
/// </summary>
[Authorize]
public class SessionHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(userId));
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(userId));
        }
        await base.OnDisconnectedAsync(exception);
    }

    public static string GroupName(string userId) => $"user-{userId}";
    public static string GroupName(Guid userId) => $"user-{userId}";
}

/// <summary>
/// Strongly-typed event names broadcast on SessionHub. Keep frontend in sync with these strings.
/// </summary>
public static class SessionHubEvents
{
    public const string SessionSuperseded = "SessionSuperseded";

    /// <summary>A new notification was created — clients refresh the bell's unread-count badge.</summary>
    public const string NotificationReceived = "NotificationReceived";
}

public record SessionSupersededPayload(
    string Reason,
    string NewSessionId,
    DateTimeOffset OccurredAt);

public record NotificationReceivedPayload(
    string Channel,
    string Subject,
    DateTimeOffset OccurredAt);
