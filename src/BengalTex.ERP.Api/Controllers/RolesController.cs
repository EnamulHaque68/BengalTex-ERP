using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Role.Commands;
using BengalTex.ERP.Application.Role.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IMediator _mediator;

    public RolesController(IMediator mediator) => _mediator = mediator;

    /// <summary>GET /api/roles — list all roles (system first, then alphabetical).</summary>
    [HttpGet]
    [HasPermission(Permissions.Roles.View)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetRolesQuery(), ct);
        return Ok(result);
    }

    /// <summary>GET /api/roles/{id} — single role with member count and permissions.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Roles.View)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetRoleByIdQuery(id), ct);
        return Ok(result);
    }

    /// <summary>POST /api/roles — create a custom role. System roles are seeded only.</summary>
    [HttpPost]
    [HasPermission(Permissions.Roles.Create)]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateRoleCommand(request.Name, request.Description), ct);
        return Ok(result);
    }

    /// <summary>PUT /api/roles/{id} — rename / re-describe. Blocked for system roles.</summary>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Roles.Edit)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoleRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateRoleCommand(id, request.Name, request.Description), ct);
        return Ok(result);
    }

    /// <summary>DELETE /api/roles/{id} — delete. Blocked for system roles or roles with members.</summary>
    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Roles.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteRoleCommand(id), ct);
        return Ok(result);
    }

    /// <summary>PUT /api/roles/{id}/permissions — replace the role's permission set.</summary>
    [HttpPut("{id:guid}/permissions")]
    [HasPermission(Permissions.Roles.ManagePermissions)]
    public async Task<IActionResult> UpdatePermissions(
        Guid id, [FromBody] UpdateRolePermissionsRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateRolePermissionsCommand(
            id, request.Permissions ?? new List<string>()
        ), ct);
        return Ok(result);
    }
}

public record CreateRoleRequest(string Name, string? Description);
public record UpdateRoleRequest(string Name, string? Description);
public record UpdateRolePermissionsRequest(List<string>? Permissions);
