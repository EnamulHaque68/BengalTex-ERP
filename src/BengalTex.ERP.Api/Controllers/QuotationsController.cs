using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Quotations.Commands;
using BengalTex.ERP.Application.Quotations.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/quotations")]
[Authorize]
public class QuotationsController : ControllerBase
{
    private readonly IMediator _mediator;
    public QuotationsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Quotations.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] int? customerId = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetQuotationsQuery(parameters, customerId, status), ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.Quotations.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetQuotationByIdQuery(id), ct));

    [HttpPost]
    [HasPermission(Permissions.Quotations.Create)]
    public async Task<IActionResult> Create([FromBody] CreateQuotationCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.Quotations.Edit)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateQuotationCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("Route id and body id do not match.");
        return Ok(await _mediator.Send(command, ct));
    }

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.Quotations.Delete)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteQuotationCommand(id), ct));

    [HttpPost("{id:long}/send")]
    [HasPermission(Permissions.Quotations.Send)]
    public async Task<IActionResult> Send(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new SendQuotationCommand(id), ct));

    [HttpPost("{id:long}/accept")]
    [HasPermission(Permissions.Quotations.Send)]
    public async Task<IActionResult> Accept(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new DecideQuotationCommand(id, true), ct));

    [HttpPost("{id:long}/reject")]
    [HasPermission(Permissions.Quotations.Send)]
    public async Task<IActionResult> Reject(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new DecideQuotationCommand(id, false), ct));

    [HttpPost("{id:long}/revise")]
    [HasPermission(Permissions.Quotations.Create)]
    public async Task<IActionResult> Revise(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new ReviseQuotationCommand(id), ct));

    [HttpPost("{id:long}/convert")]
    [HasPermission(Permissions.Quotations.Convert)]
    public async Task<IActionResult> Convert(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new ConvertQuotationToSalesOrderCommand(id), ct));

    /// <summary>Generates a draft Proforma Invoice from an accepted quotation (pre-order flow for LC / advance).</summary>
    [HttpPost("{id:long}/generate-proforma")]
    [HasPermission(Permissions.Quotations.Convert)]
    public async Task<IActionResult> GenerateProforma(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new GenerateProformaFromQuotationCommand(id), ct));
}
