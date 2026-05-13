using BengalTex.ERP.Api.Hubs;
using BengalTex.ERP.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace BengalTex.ERP.Api.Services;

/// <summary>
/// SignalR-backed implementation of ISessionBroadcaster.
/// Lives in the Api layer because the SignalR hub itself is hosted here.
/// </summary>
public class SessionBroadcaster : ISessionBroadcaster
{
    private readonly IHubContext<SessionHub> _hubContext;

    public SessionBroadcaster(IHubContext<SessionHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifySessionSupersededAsync(Guid userId, string newSessionId, CancellationToken ct = default)
    {
        var payload = new SessionSupersededPayload(
            Reason: "A new login was detected on another device.",
            NewSessionId: newSessionId,
            OccurredAt: DateTimeOffset.UtcNow);

        return _hubContext.Clients
            .Group(SessionHub.GroupName(userId))
            .SendAsync(SessionHubEvents.SessionSuperseded, payload, ct);
    }
}
