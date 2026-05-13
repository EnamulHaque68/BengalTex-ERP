namespace BengalTex.ERP.Application.Common.Interfaces;

/// <summary>
/// Broadcasts session-lifecycle events to connected clients (typically via SignalR).
/// Defined in Application so Infrastructure can depend on it without referencing
/// the SignalR hub directly. Implementation lives in the Api layer where the hub
/// is hosted.
/// </summary>
public interface ISessionBroadcaster
{
    /// <summary>
    /// Notifies all currently-connected clients of the given user that their session
    /// has been replaced by a newer login. Clients should clear local tokens and redirect
    /// to login on receipt.
    /// </summary>
    Task NotifySessionSupersededAsync(Guid userId, string newSessionId, CancellationToken ct = default);
}
