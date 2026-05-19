using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.VatChallan.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/vat-challans")]
[Authorize]
public class VatChallansController : ControllerBase
{
    private readonly IMediator _mediator;

    public VatChallansController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Invoices.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] int? customerId = null,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetVatChallansQuery(parameters, customerId, fromDate, toDate), ct);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.Invoices.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetVatChallanByIdQuery(id), ct);
        return Ok(result);
    }
}
