using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.OpeningStock.Commands;
using BengalTex.ERP.Application.OpeningStock.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

/// <summary>
/// Opening-stock seeding (go-live) — raw materials AND finished products at a known unit cost.
/// Reuses the Inventory permission group.
/// </summary>
[ApiController]
[Route("api/opening-stock")]
[Authorize]
public class OpeningStockController : ControllerBase
{
    private readonly IMediator _mediator;
    public OpeningStockController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Inventory.View)]
    public async Task<IActionResult> GetAll([FromQuery] PagedQueryParameters parameters, [FromQuery] string? status = null, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetOpeningStockEntriesQuery(parameters, status), ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.Inventory.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetOpeningStockEntryByIdQuery(id), ct));

    [HttpPost]
    [HasPermission(Permissions.Inventory.Adjust)]
    public async Task<IActionResult> Create([FromBody] CreateOpeningStockCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.Inventory.Adjust)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateOpeningStockCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("Route id and body id do not match.");
        return Ok(await _mediator.Send(command, ct));
    }

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.Inventory.Adjust)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteOpeningStockCommand(id), ct));

    [HttpPost("{id:long}/post")]
    [HasPermission(Permissions.Inventory.Adjust)]
    public async Task<IActionResult> Post(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new PostOpeningStockCommand(id), ct));
}
