using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.CustomerReturnNote.Commands;
using BengalTex.ERP.Application.CustomerReturnNote.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/customer-return-notes")]
[Authorize]
public class CustomerReturnNotesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CustomerReturnNotesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Returns.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] long? deliveryNoteId = null,
        [FromQuery] int? customerId = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetCustomerReturnNotesQuery(parameters, deliveryNoteId, customerId, status), ct);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.Returns.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCustomerReturnNoteByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Returns.Create)]
    public async Task<IActionResult> Create([FromBody] CreateCustomerReturnNoteRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateCustomerReturnNoteCommand(
            request.DeliveryNoteId, request.ReturnWarehouseId, request.ReturnDate,
            request.VehicleNumber, request.Reason, request.Notes, request.Lines
        ), ct);
        return Ok(result);
    }

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.Returns.Edit)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateCustomerReturnNoteRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateCustomerReturnNoteCommand(
            id, request.ReturnWarehouseId, request.ReturnDate,
            request.VehicleNumber, request.Reason, request.Notes, request.Lines
        ), ct);
        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.Returns.Delete)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteCustomerReturnNoteCommand(id), ct);
        return Ok(result);
    }

    [HttpPost("{id:long}/post")]
    [HasPermission(Permissions.Returns.Post)]
    public async Task<IActionResult> Post(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new PostCustomerReturnNoteCommand(id), ct);
        return Ok(result);
    }
}

public record CreateCustomerReturnNoteRequest(
    long DeliveryNoteId,
    int ReturnWarehouseId,
    DateOnly ReturnDate,
    string? VehicleNumber,
    string? Reason,
    string? Notes,
    IReadOnlyList<CustomerReturnNoteLineInput> Lines);

public record UpdateCustomerReturnNoteRequest(
    int ReturnWarehouseId,
    DateOnly ReturnDate,
    string? VehicleNumber,
    string? Reason,
    string? Notes,
    IReadOnlyList<CustomerReturnNoteLineInput> Lines);
