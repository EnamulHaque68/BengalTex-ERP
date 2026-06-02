using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Dashboard.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;
    public DashboardController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Single-round-trip snapshot of the entire factory: hero strip + role-gated
    /// section blocks + top "needs attention" items. Hero always visible to any
    /// authenticated user; sections nullable based on caller's
    /// <c>Permissions.Dashboard.*</c> grants (checked inside the handler).
    /// </summary>
    [HttpGet("snapshot")]
    public async Task<IActionResult> Snapshot(CancellationToken ct)
        => Ok(await _mediator.Send(new GetDashboardSnapshotQuery(), ct));
}
