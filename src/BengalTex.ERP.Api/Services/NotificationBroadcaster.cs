using BengalTex.ERP.Api.Hubs;
using BengalTex.ERP.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace BengalTex.ERP.Api.Services;

/// <summary>
/// SignalR-backed implementation of <see cref="INotificationBroadcaster"/>. Reuses the existing
/// <see cref="SessionHub"/>. Notifications are a global log (no per-user inbox), so the event is
/// sent to every connected client; each bell then re-reads its own "since last seen" count.
/// </summary>
public class NotificationBroadcaster : INotificationBroadcaster
{
    private readonly IHubContext<SessionHub> _hubContext;

    public NotificationBroadcaster(IHubContext<SessionHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task BroadcastNotificationAsync(string channel, string subject, CancellationToken ct = default)
    {
        var payload = new NotificationReceivedPayload(channel, subject, DateTimeOffset.UtcNow);
        return _hubContext.Clients.All.SendAsync(SessionHubEvents.NotificationReceived, payload, ct);
    }
}
