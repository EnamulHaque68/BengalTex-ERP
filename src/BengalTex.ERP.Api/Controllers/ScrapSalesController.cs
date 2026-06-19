using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.ScrapSales;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

/// <summary>
/// Scrap / wastage sales — reusable production scrap sold for cash/bank.
/// Financial-only (not carried as stock): posting records income (Dr Cash/Bank, Cr Scrap Sales Income).
/// Reuses the Wastage permission group.
/// </summary>
[ApiController]
[Route("api/scrap-sales")]
[Authorize]
public class ScrapSalesController : ControllerBase
{
    private readonly IMediator _mediator;
    public ScrapSalesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Wastage.View)]
    public async Task<IActionResult> GetAll([FromQuery] PagedQueryParameters parameters, [FromQuery] string? status = null, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetScrapSalesQuery(parameters, status), ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.Wastage.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetScrapSaleByIdQuery(id), ct));

    [HttpPost]
    [HasPermission(Permissions.Wastage.Create)]
    public async Task<IActionResult> Create([FromBody] CreateScrapSaleCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.Wastage.Edit)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateScrapSaleCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("Route id and body id do not match.");
        return Ok(await _mediator.Send(command, ct));
    }

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.Wastage.Delete)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteScrapSaleCommand(id), ct));

    [HttpPost("{id:long}/post")]
    [HasPermission(Permissions.Wastage.Create)]
    public async Task<IActionResult> Post(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new PostScrapSaleCommand(id), ct));
}
