using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.SupplierReturnNote.Commands;
using BengalTex.ERP.Application.SupplierReturnNote.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/supplier-return-notes")]
[Authorize]
public class SupplierReturnNotesController : ControllerBase
{
    private readonly IMediator _mediator;

    public SupplierReturnNotesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Returns.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] long? goodsReceiptNoteId = null,
        [FromQuery] int? supplierId = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetSupplierReturnNotesQuery(parameters, goodsReceiptNoteId, supplierId, status), ct);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.Returns.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSupplierReturnNoteByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Returns.Create)]
    public async Task<IActionResult> Create([FromBody] CreateSupplierReturnNoteRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateSupplierReturnNoteCommand(
            request.GoodsReceiptNoteId, request.ReturnFromWarehouseId, request.ReturnDate,
            request.VehicleNumber, request.Reason, request.Notes, request.Lines
        ), ct);
        return Ok(result);
    }

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.Returns.Edit)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateSupplierReturnNoteRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateSupplierReturnNoteCommand(
            id, request.ReturnFromWarehouseId, request.ReturnDate,
            request.VehicleNumber, request.Reason, request.Notes, request.Lines
        ), ct);
        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.Returns.Delete)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteSupplierReturnNoteCommand(id), ct);
        return Ok(result);
    }

    [HttpPost("{id:long}/post")]
    [HasPermission(Permissions.Returns.Post)]
    public async Task<IActionResult> Post(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new PostSupplierReturnNoteCommand(id), ct);
        return Ok(result);
    }
}

public record CreateSupplierReturnNoteRequest(
    long GoodsReceiptNoteId,
    int ReturnFromWarehouseId,
    DateOnly ReturnDate,
    string? VehicleNumber,
    string? Reason,
    string? Notes,
    IReadOnlyList<SupplierReturnNoteLineInput> Lines);

public record UpdateSupplierReturnNoteRequest(
    int ReturnFromWarehouseId,
    DateOnly ReturnDate,
    string? VehicleNumber,
    string? Reason,
    string? Notes,
    IReadOnlyList<SupplierReturnNoteLineInput> Lines);
