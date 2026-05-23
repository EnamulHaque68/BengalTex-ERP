using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Samples.Commands;
using BengalTex.ERP.Application.Samples.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/samples")]
[Authorize]
public class SamplesController : ControllerBase
{
    private readonly IMediator _mediator;
    public SamplesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Samples.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] int? customerId = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetSamplesQuery(parameters, customerId, status), ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.Samples.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetSampleByIdQuery(id), ct));

    [HttpPost]
    [HasPermission(Permissions.Samples.Create)]
    public async Task<IActionResult> Create([FromBody] CreateSampleCommand command, CancellationToken ct)
        => Ok(await _mediator.Send(command, ct));

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.Samples.Edit)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateSampleCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("Route id and body id do not match.");
        return Ok(await _mediator.Send(command, ct));
    }

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.Samples.Delete)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteSampleCommand(id), ct));

    [HttpPost("{id:long}/start-development")]
    [HasPermission(Permissions.Samples.Manage)]
    public async Task<IActionResult> StartDevelopment(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new StartSampleDevelopmentCommand(id), ct));

    [HttpPost("{id:long}/submit")]
    [HasPermission(Permissions.Samples.Manage)]
    public async Task<IActionResult> Submit(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new SubmitSampleCommand(id), ct));

    [HttpPost("{id:long}/decide")]
    [HasPermission(Permissions.Samples.Manage)]
    public async Task<IActionResult> Decide(long id, [FromBody] DecideSampleRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new DecideSampleCommand(id, request.Approve, request.Feedback), ct));
}

public record DecideSampleRequest(bool Approve, string? Feedback);
