using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Notifications.Commands;
using BengalTex.ERP.Application.Notifications.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Notifications.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] string? channel = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetNotificationsQuery(parameters, channel, status), ct);
        return Ok(result);
    }

    /// <summary>
    /// Unread-count for the notification bell badge. <paramref name="since"/> is the timestamp the
    /// user last opened the notifications page (client-tracked); omit it to count everything.
    /// Only [Authorize] — it returns a number, not content, so every logged-in user's bell works.
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount([FromQuery] DateTimeOffset? since = null, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetNotificationUnreadCountQuery(since), ct);
        return Ok(result);
    }

    [HttpPost("test")]
    [HasPermission(Permissions.Notifications.Send)]
    public async Task<IActionResult> SendTest([FromBody] SendTestNotificationCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }
}
