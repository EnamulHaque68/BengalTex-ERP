using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.GatePasses.Commands;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/gate-passes")]
[Authorize]
public class GatePassesController : ControllerBase
{
    private readonly IMediator _mediator;
    public GatePassesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.GatePasses.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] string? status = null,
        [FromQuery] string? type = null,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetGatePassesQuery(parameters, status, type, fromDate, toDate), ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.GatePasses.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetGatePassByIdQuery(id), ct));

    [HttpPost]
    [HasPermission(Permissions.GatePasses.Create)]
    public async Task<IActionResult> Create([FromBody] CreateGatePassCommand cmd, CancellationToken ct)
        => Ok(await _mediator.Send(cmd, ct));

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.GatePasses.Edit)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateGatePassBody body, CancellationToken ct)
        => Ok(await _mediator.Send(new UpdateGatePassCommand(
            id, body.PassDate, body.PassTime, body.Type, body.Direction,
            body.VehicleNumber, body.DriverName, body.DriverPhone, body.DriverNidNumber, body.TransporterName,
            body.VisitorName, body.VisitorPhone, body.VisitorOrganization, body.VisitorPurpose,
            body.ItemDescription, body.Quantity, body.FromLocation, body.ToLocation,
            body.SourceType, body.SourceId, body.SourceCode,
            body.ApprovedByUser, body.ExpectedReturnDate, body.Notes), ct));

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.GatePasses.Delete)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteGatePassCommand(id), ct));

    [HttpPost("{id:long}/close")]
    [HasPermission(Permissions.GatePasses.Close)]
    public async Task<IActionResult> Close(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new CloseGatePassCommand(id), ct));

    [HttpPost("{id:long}/mark-returned")]
    [HasPermission(Permissions.GatePasses.Close)]
    public async Task<IActionResult> MarkReturned(long id, [FromBody] MarkReturnedBody? body, CancellationToken ct)
        => Ok(await _mediator.Send(new MarkGatePassReturnedCommand(id, body?.ReturnNotes), ct));

    [HttpPost("{id:long}/cancel")]
    [HasPermission(Permissions.GatePasses.Close)]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new CancelGatePassCommand(id), ct));
}

public record UpdateGatePassBody(
    DateOnly PassDate, TimeOnly? PassTime, string Type, string Direction,
    string? VehicleNumber, string? DriverName, string? DriverPhone, string? DriverNidNumber, string? TransporterName,
    string? VisitorName, string? VisitorPhone, string? VisitorOrganization, string? VisitorPurpose,
    string? ItemDescription, string? Quantity, string? FromLocation, string? ToLocation,
    string? SourceType, long? SourceId, string? SourceCode,
    string? ApprovedByUser, DateOnly? ExpectedReturnDate, string? Notes);

public record MarkReturnedBody(string? ReturnNotes);
