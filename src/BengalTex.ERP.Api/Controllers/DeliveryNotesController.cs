using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.DeliveryNote.Commands;
using BengalTex.ERP.Application.DeliveryNote.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/delivery-notes")]
[Authorize]
public class DeliveryNotesController : ControllerBase
{
    private readonly IMediator _mediator;

    public DeliveryNotesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.DeliveryNotes.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] long? salesOrderId = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetDeliveryNotesQuery(parameters, salesOrderId, status), ct);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.DeliveryNotes.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDeliveryNoteByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.DeliveryNotes.Create)]
    public async Task<IActionResult> Create([FromBody] CreateDeliveryNoteRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateDeliveryNoteCommand(
            request.SalesOrderId, request.DispatchDate, request.DispatchWarehouseId,
            request.VehicleNumber, request.DriverContact, request.DeliveryAddress,
            request.Notes, request.Lines,
            request.PlannedDeliveryDate, request.TransportCompany, request.DriverName
        ), ct);
        return Ok(result);
    }

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.DeliveryNotes.Edit)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateDeliveryNoteRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateDeliveryNoteCommand(
            id, request.DispatchDate, request.DispatchWarehouseId,
            request.VehicleNumber, request.DriverContact, request.DeliveryAddress,
            request.Notes, request.Lines,
            request.PlannedDeliveryDate, request.TransportCompany, request.DriverName
        ), ct);
        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.DeliveryNotes.Delete)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteDeliveryNoteCommand(id), ct);
        return Ok(result);
    }

    [HttpPost("{id:long}/post")]
    [HasPermission(Permissions.DeliveryNotes.Post)]
    public async Task<IActionResult> Post(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new PostDeliveryNoteCommand(id), ct);
        return Ok(result);
    }
}

public record CreateDeliveryNoteRequest(
    long SalesOrderId,
    DateOnly DispatchDate,
    int DispatchWarehouseId,
    string? VehicleNumber,
    string? DriverContact,
    string? DeliveryAddress,
    string? Notes,
    IReadOnlyList<DeliveryNoteLineInput> Lines,
    DateOnly? PlannedDeliveryDate = null,
    string? TransportCompany = null,
    string? DriverName = null);

public record UpdateDeliveryNoteRequest(
    DateOnly DispatchDate,
    int DispatchWarehouseId,
    string? VehicleNumber,
    string? DriverContact,
    string? DeliveryAddress,
    string? Notes,
    IReadOnlyList<DeliveryNoteLineInput> Lines,
    DateOnly? PlannedDeliveryDate = null,
    string? TransportCompany = null,
    string? DriverName = null);
