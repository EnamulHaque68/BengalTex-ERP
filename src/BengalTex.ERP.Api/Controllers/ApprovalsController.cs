using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Approvals.Commands;
using BengalTex.ERP.Application.Approvals.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/approvals")]
[Authorize]
public class ApprovalsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ApprovalsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Pending requests the current user can act on.</summary>
    [HttpGet("inbox")]
    [HasPermission(Permissions.Approvals.View)]
    public async Task<IActionResult> Inbox(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetApprovalInboxQuery(), ct);
        return Ok(result);
    }

    /// <summary>All approval requests, optionally filtered by status.</summary>
    [HttpGet]
    [HasPermission(Permissions.Approvals.View)]
    public async Task<IActionResult> GetAll([FromQuery] string? status, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetApprovalsQuery(status), ct);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.Approvals.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetApprovalByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPost("{id:long}/approve")]
    [HasPermission(Permissions.Approvals.Act)]
    public async Task<IActionResult> Approve(long id, [FromBody] ApprovalDecisionRequest? body, CancellationToken ct)
    {
        var result = await _mediator.Send(new DecideApprovalRequestCommand(id, true, body?.Comment), ct);
        return Ok(result);
    }

    [HttpPost("{id:long}/reject")]
    [HasPermission(Permissions.Approvals.Act)]
    public async Task<IActionResult> Reject(long id, [FromBody] ApprovalDecisionRequest? body, CancellationToken ct)
    {
        var result = await _mediator.Send(new DecideApprovalRequestCommand(id, false, body?.Comment), ct);
        return Ok(result);
    }
}

public record ApprovalDecisionRequest(string? Comment);
