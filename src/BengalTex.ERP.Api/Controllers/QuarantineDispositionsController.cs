using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.QuarantineDisposition.Commands;
using BengalTex.ERP.Application.QuarantineDisposition.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/quarantine-dispositions")]
[Authorize]
public class QuarantineDispositionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public QuarantineDispositionsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Qc.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] string? dispositionType = null,
        [FromQuery] int? quarantineWarehouseId = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var res = await _mediator.Send(
            new GetQuarantineDispositionsQuery(parameters, dispositionType, quarantineWarehouseId, status), ct);
        return Ok(res);
    }

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.Qc.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var res = await _mediator.Send(new GetQuarantineDispositionByIdQuery(id), ct);
        return Ok(res);
    }

    [HttpPost]
    [HasPermission(Permissions.Qc.Create)]
    public async Task<IActionResult> Create([FromBody] CreateQuarantineDispositionRequest request, CancellationToken ct)
    {
        var res = await _mediator.Send(new CreateQuarantineDispositionCommand(
            request.DispositionType, request.DispositionDate, request.QuarantineWarehouseId,
            request.DestinationWarehouseId, request.Reason, request.Notes, request.Lines
        ), ct);
        return Ok(res);
    }

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.Qc.Edit)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateQuarantineDispositionRequest request, CancellationToken ct)
    {
        var res = await _mediator.Send(new UpdateQuarantineDispositionCommand(
            id, request.DispositionDate, request.DestinationWarehouseId,
            request.Reason, request.Notes, request.Lines
        ), ct);
        return Ok(res);
    }

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.Qc.Delete)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var res = await _mediator.Send(new DeleteQuarantineDispositionCommand(id), ct);
        return Ok(res);
    }

    [HttpPost("{id:long}/post")]
    [HasPermission(Permissions.Qc.Post)]
    public async Task<IActionResult> Post(long id, CancellationToken ct)
    {
        var res = await _mediator.Send(new PostQuarantineDispositionCommand(id), ct);
        return Ok(res);
    }
}

public record CreateQuarantineDispositionRequest(
    string DispositionType,
    DateOnly DispositionDate,
    int QuarantineWarehouseId,
    int? DestinationWarehouseId,
    string? Reason,
    string? Notes,
    IReadOnlyList<QuarantineDispositionLineInput> Lines);

public record UpdateQuarantineDispositionRequest(
    DateOnly DispositionDate,
    int? DestinationWarehouseId,
    string? Reason,
    string? Notes,
    IReadOnlyList<QuarantineDispositionLineInput> Lines);
