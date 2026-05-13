using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.User.Commands;
using BengalTex.ERP.Application.User.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator) => _mediator = mediator;

    /// <summary>GET /api/users — paginated user list with optional search.</summary>
    [HttpGet]
    [HasPermission(Permissions.Users.View)]
    public async Task<IActionResult> GetAll([FromQuery] PagedQueryParameters parameters, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetUsersQuery(parameters), ct);
        return Ok(result);
    }

    /// <summary>GET /api/users/{id} — get single user details.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Users.View)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetUserByIdQuery(id), ct);
        return Ok(result);
    }

    /// <summary>POST /api/users — create a user and assign roles in one call.</summary>
    [HttpPost]
    [HasPermission(Permissions.Users.Create)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateUserCommand(
            request.UserName, request.Email, request.FullName,
            request.Password, request.ConfirmPassword,
            request.FactoryId, request.Roles ?? new List<string>()
        ), ct);
        return Ok(result);
    }

    /// <summary>PUT /api/users/{id} — update basic profile (no password, no roles).</summary>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Users.Edit)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateUserCommand(
            id, request.UserName, request.Email, request.FullName, request.FactoryId
        ), ct);
        return Ok(result);
    }

    /// <summary>PATCH /api/users/{id}/active — toggle active state (acts as soft delete).</summary>
    [HttpPatch("{id:guid}/active")]
    [HasPermission(Permissions.Users.Edit)]
    public async Task<IActionResult> SetActive(Guid id, [FromBody] SetActiveRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new SetUserActiveCommand(id, request.IsActive), ct);
        return Ok(result);
    }

    /// <summary>PUT /api/users/{id}/roles — replace the user's roles with the given set.</summary>
    [HttpPut("{id:guid}/roles")]
    [HasPermission(Permissions.Users.ManageRoles)]
    public async Task<IActionResult> UpdateRoles(Guid id, [FromBody] UpdateRolesRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateUserRolesCommand(id, request.Roles ?? new List<string>()), ct);
        return Ok(result);
    }

    /// <summary>POST /api/users/{id}/reset-password — admin force-reset password.</summary>
    [HttpPost("{id:guid}/reset-password")]
    [HasPermission(Permissions.Users.Edit)]
    public async Task<IActionResult> AdminResetPassword(
        Guid id, [FromBody] AdminResetPasswordRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new AdminResetPasswordCommand(
            id, request.NewPassword, request.ConfirmPassword
        ), ct);
        return Ok(result);
    }
}

// ─── Request DTOs ────────────────────────────────────────────────────────────

public record CreateUserRequest(
    string UserName,
    string Email,
    string FullName,
    string Password,
    string ConfirmPassword,
    int? FactoryId,
    List<string>? Roles);

public record UpdateUserRequest(
    string UserName,
    string Email,
    string FullName,
    int? FactoryId);

public record SetActiveRequest(bool IsActive);

public record UpdateRolesRequest(List<string>? Roles);

public record AdminResetPasswordRequest(
    string NewPassword,
    string ConfirmPassword);
