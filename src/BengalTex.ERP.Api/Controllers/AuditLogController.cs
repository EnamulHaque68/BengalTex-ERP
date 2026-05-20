using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.AuditLog.Queries;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/audit-log")]
[Authorize]
public class AuditLogController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuditLogController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.AuditLog.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] string? entityType = null,
        [FromQuery] string? action = null,
        [FromQuery] string? userName = null,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetAuditLogQuery(parameters, entityType, action, userName, fromDate, toDate), ct);
        return Ok(result);
    }

    [HttpGet("entity-types")]
    [HasPermission(Permissions.AuditLog.View)]
    public async Task<IActionResult> GetEntityTypes(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAuditLogEntityTypesQuery(), ct);
        return Ok(result);
    }
}
