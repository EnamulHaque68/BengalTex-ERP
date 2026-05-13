using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Permission.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/permissions")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PermissionsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// GET /api/permissions — catalog of all defined permissions, grouped by category.
    /// Drives the role-permission picker UI in the admin frontend.
    /// </summary>
    [HttpGet]
    [HasPermission(Permissions.Roles.View)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAllPermissionsQuery(), ct);
        return Ok(result);
    }
}
