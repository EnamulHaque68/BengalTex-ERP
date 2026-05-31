using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.JobCards.Commands;
using BengalTex.ERP.Application.JobCards.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/job-cards")]
[Authorize]
public class JobCardsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IQrCodeService _qr;
    public JobCardsController(IMediator mediator, IQrCodeService qr)
    {
        _mediator = mediator;
        _qr = qr;
    }

    [HttpGet]
    [HasPermission(Permissions.JobCards.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] string? status = null,
        [FromQuery] long? productionOrderId = null,
        [FromQuery] int? machineId = null,
        [FromQuery] int? operatorEmployeeId = null,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetJobCardsQuery(
            parameters, status, productionOrderId, machineId, operatorEmployeeId, fromDate, toDate), ct));

    [HttpGet("board-counts")]
    [HasPermission(Permissions.JobCards.View)]
    public async Task<IActionResult> BoardCounts(CancellationToken ct)
        => Ok(await _mediator.Send(new GetJobCardBoardCountsQuery(), ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.JobCards.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetJobCardByIdQuery(id), ct));

    [HttpPost]
    [HasPermission(Permissions.JobCards.Create)]
    public async Task<IActionResult> Create([FromBody] CreateJobCardRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateJobCardCommand(
            request.ProductionOrderId, request.ProductionStageId, request.BatchNumber,
            request.Quantity, request.MachineId, request.OperatorEmployeeId, request.Notes), ct));

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.JobCards.Edit)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateJobCardRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new UpdateJobCardCommand(
            id, request.BatchNumber, request.Quantity, request.MachineId, request.OperatorEmployeeId, request.Notes), ct));

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.JobCards.Delete)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteJobCardCommand(id), ct));

    /// <summary>
    /// Operator scan endpoint — accepts JobCard Id OR Code (so a QR scan can post directly).
    /// Drives the Start/Pause/Resume/Complete/QcCheck/Cancel state machine.
    /// </summary>
    [HttpPost("scan")]
    [HasPermission(Permissions.JobCards.Scan)]
    public async Task<IActionResult> Scan([FromBody] ScanJobCardRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new ScanJobCardCommand(
            request.JobCardId, request.Code, request.ScanType,
            request.Quantity, request.RejectedQuantity, request.Notes), ct));

    /// <summary>Returns a printable PNG QR code for the given job card (encodes its Code).</summary>
    [HttpGet("{id:long}/qr")]
    [HasPermission(Permissions.JobCards.View)]
    public async Task<IActionResult> GetQr(long id, CancellationToken ct)
    {
        var jc = await _mediator.Send(new GetJobCardByIdQuery(id), ct);
        if (!jc.Success || jc.Data is null) return NotFound(jc);
        var png = _qr.GeneratePng(jc.Data.Code, pixelsPerModule: 8);
        return File(png, "image/png", $"{jc.Data.Code}.png");
    }
}

public record CreateJobCardRequest(
    long ProductionOrderId,
    long? ProductionStageId,
    string? BatchNumber,
    decimal Quantity,
    int? MachineId,
    int? OperatorEmployeeId,
    string? Notes);

public record UpdateJobCardRequest(
    string? BatchNumber,
    decimal Quantity,
    int? MachineId,
    int? OperatorEmployeeId,
    string? Notes);

public record ScanJobCardRequest(
    long? JobCardId,
    string? Code,
    string ScanType,
    decimal? Quantity,
    decimal? RejectedQuantity,
    string? Notes);
