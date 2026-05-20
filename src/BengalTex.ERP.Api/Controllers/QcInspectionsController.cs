using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.QcInspection.Commands;
using BengalTex.ERP.Application.QcInspection.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/qc-inspections")]
[Authorize]
public class QcInspectionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public QcInspectionsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Qc.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] string? sourceType = null,
        [FromQuery] string? status = null,
        [FromQuery] string? result = null,
        CancellationToken ct = default)
    {
        var res = await _mediator.Send(
            new GetQcInspectionsQuery(parameters, sourceType, status, result), ct);
        return Ok(res);
    }

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.Qc.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var res = await _mediator.Send(new GetQcInspectionByIdQuery(id), ct);
        return Ok(res);
    }

    [HttpPost]
    [HasPermission(Permissions.Qc.Create)]
    public async Task<IActionResult> Create([FromBody] CreateQcInspectionRequest request, CancellationToken ct)
    {
        var res = await _mediator.Send(new CreateQcInspectionCommand(
            request.SourceType, request.GoodsReceiptNoteId, request.ProductionOrderId,
            request.InspectionDate, request.QuarantineWarehouseId,
            request.InspectedBy, request.Notes, request.Lines
        ), ct);
        return Ok(res);
    }

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.Qc.Edit)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateQcInspectionRequest request, CancellationToken ct)
    {
        var res = await _mediator.Send(new UpdateQcInspectionCommand(
            id, request.InspectionDate, request.QuarantineWarehouseId,
            request.InspectedBy, request.Notes, request.Lines
        ), ct);
        return Ok(res);
    }

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.Qc.Delete)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var res = await _mediator.Send(new DeleteQcInspectionCommand(id), ct);
        return Ok(res);
    }

    [HttpPost("{id:long}/post")]
    [HasPermission(Permissions.Qc.Post)]
    public async Task<IActionResult> Post(long id, CancellationToken ct)
    {
        var res = await _mediator.Send(new PostQcInspectionCommand(id), ct);
        return Ok(res);
    }
}

public record CreateQcInspectionRequest(
    string SourceType,
    long? GoodsReceiptNoteId,
    long? ProductionOrderId,
    DateOnly InspectionDate,
    int QuarantineWarehouseId,
    string? InspectedBy,
    string? Notes,
    IReadOnlyList<QcInspectionLineInput> Lines);

public record UpdateQcInspectionRequest(
    DateOnly InspectionDate,
    int QuarantineWarehouseId,
    string? InspectedBy,
    string? Notes,
    IReadOnlyList<QcInspectionLineInput> Lines);
