namespace BengalTex.ERP.Application.Common.Interfaces;

/// <summary>
/// Pushes a real-time "a notification was just created" signal to connected clients (via SignalR)
/// so the notification bell can refresh its unread-count badge immediately, rather than waiting
/// for the next poll. Defined in Application so Infrastructure can depend on it without referencing
/// the SignalR hub; the implementation lives in the Api layer where the hub is hosted.
/// </summary>
public interface INotificationBroadcaster
{
    /// <summary>
    /// Broadcasts a lightweight notification event to all connected clients. Best-effort —
    /// failures must never break the originating command.
    /// </summary>
    Task BroadcastNotificationAsync(string channel, string subject, CancellationToken ct = default);
}
