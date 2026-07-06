using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Accounting.Dimensions;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

/// <summary>Phase A3 — cost / profit center master (the primary accounting dimension).</summary>
[ApiController]
[Route("api/cost-centers")]
[Authorize]
public class CostCentersController : ControllerBase
{
    private readonly IMediator _mediator;
    public CostCentersController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Accounting.View)]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetCostCentersQuery(includeInactive), ct));

    [HttpPost]
    [HasPermission(Permissions.Accounting.ManageDimensions)]
    public async Task<IActionResult> Create([FromBody] CreateCostCenterCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Accounting.ManageDimensions)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCostCenterCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("Route id and body id do not match.");
        return Ok(await _mediator.Send(command, ct));
    }
}
