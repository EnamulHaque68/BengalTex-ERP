using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Wastage.Commands;
using BengalTex.ERP.Application.Wastage.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/wastage-entries")]
[Authorize]
public class WastageEntriesController : ControllerBase
{
    private readonly IMediator _mediator;
    public WastageEntriesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Wastage.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] int? rawMaterialId = null,
        [FromQuery] int? wastageReasonId = null,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetWastageEntriesQuery(parameters, rawMaterialId, wastageReasonId, fromDate, toDate), ct));

    [HttpGet("summary")]
    [HasPermission(Permissions.Wastage.View)]
    public async Task<IActionResult> Summary([FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate, CancellationToken ct)
        => Ok(await _mediator.Send(new GetWastageSummaryQuery(fromDate, toDate), ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.Wastage.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetWastageEntryByIdQuery(id), ct));

    [HttpPost]
    [HasPermission(Permissions.Wastage.Create)]
    public async Task<IActionResult> Create([FromBody] CreateWastageEntryCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.Wastage.Edit)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateWastageEntryCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("Route id and body id do not match.");
        return Ok(await _mediator.Send(command, ct));
    }

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.Wastage.Delete)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteWastageEntryCommand(id), ct));
}

[ApiController]
[Route("api/wastage-reasons")]
[Authorize]
public class WastageReasonsController : ControllerBase
{
    private readonly IMediator _mediator;
    public WastageReasonsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Wastage.View)]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetWastageReasonsQuery(includeInactive), ct));

    [HttpPost]
    [HasPermission(Permissions.Wastage.ManageReasons)]
    public async Task<IActionResult> Create([FromBody] CreateWastageReasonCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Wastage.ManageReasons)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateWastageReasonCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("Route id and body id do not match.");
        return Ok(await _mediator.Send(command, ct));
    }

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.Wastage.ManageReasons)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteWastageReasonCommand(id), ct));
}
