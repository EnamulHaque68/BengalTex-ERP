using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Accounting.Costing;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

/// <summary>Phase A4 — absorption costing rates (labour / machine-OH / factory-OH).</summary>
[ApiController]
[Route("api/costing-rates")]
[Authorize]
public class CostingRatesController : ControllerBase
{
    private readonly IMediator _mediator;
    public CostingRatesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Accounting.View)]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetCostingRatesQuery(includeInactive), ct));

    [HttpPost]
    [HasPermission(Permissions.Accounting.ManageDimensions)]
    public async Task<IActionResult> Create([FromBody] CreateCostingRateCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Accounting.ManageDimensions)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCostingRateCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("Route id and body id do not match.");
        return Ok(await _mediator.Send(command, ct));
    }
}
