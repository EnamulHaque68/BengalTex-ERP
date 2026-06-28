using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Production.Commands;
using BengalTex.ERP.Application.Production.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/production-orders")]
[Authorize]
public class ProductionOrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductionOrdersController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Production.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] int? productId = null,
        [FromQuery] string? status = null,
        [FromQuery] long? salesOrderId = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetProductionOrdersQuery(parameters, productId, status, salesOrderId), ct);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.Production.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProductionOrderByIdQuery(id), ct);
        return Ok(result);
    }

    /// <summary>Manufacturing Calendar — production orders + holidays + weekend days for a date range.</summary>
    [HttpGet("calendar")]
    [HasPermission(Permissions.Production.View)]
    public async Task<IActionResult> Calendar(
        [FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetProductionCalendarQuery(from, to), ct));

    /// <summary>Completed productions still QC-held (remaining hold &gt; 0) — for the QC inspection picker.</summary>
    [HttpGet("awaiting-qc")]
    [HasPermission(Permissions.Production.View)]
    public async Task<IActionResult> AwaitingQc(CancellationToken ct)
        => Ok(await _mediator.Send(new GetProductionsAwaitingQcQuery(), ct));

    /// <summary>End-to-end traceability for a production run (FG → JobCards → SO → Quotation → Customer → BOM → RM + lots).</summary>
    [HttpGet("{id:long}/traceability")]
    [HasPermission(Permissions.Production.View)]
    public async Task<IActionResult> Traceability(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetProductionTraceabilityQuery(id), ct));

    [HttpPost]
    [HasPermission(Permissions.Production.Create)]
    public async Task<IActionResult> Create([FromBody] CreateProductionOrderRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateProductionOrderCommand(
            request.ProductId, request.BomId, request.Quantity,
            request.IssueWarehouseId, request.ReceiveWarehouseId,
            request.PlannedStartDate, request.PlannedEndDate, request.Notes, request.Stages,
            request.SalesOrderId, request.SalesOrderLineId, request.RequiresQc
        ), ct);
        return Ok(result);
    }

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.Production.Edit)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateProductionOrderRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateProductionOrderCommand(
            id, request.ProductId, request.BomId, request.Quantity,
            request.IssueWarehouseId, request.ReceiveWarehouseId,
            request.PlannedStartDate, request.PlannedEndDate, request.Notes, request.Stages,
            request.SalesOrderId, request.SalesOrderLineId, request.RequiresQc
        ), ct);
        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.Production.Edit)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteProductionOrderCommand(id), ct);
        return Ok(result);
    }

    [HttpPost("{id:long}/start")]
    [HasPermission(Permissions.Production.Edit)]
    public async Task<IActionResult> Start(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new StartProductionOrderCommand(id), ct);
        return Ok(result);
    }

    [HttpPost("{id:long}/complete")]
    [HasPermission(Permissions.Production.IssueMaterial)]
    public async Task<IActionResult> Complete(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new CompleteProductionOrderCommand(id), ct);
        return Ok(result);
    }

    [HttpPost("{id:long}/cancel")]
    [HasPermission(Permissions.Production.Edit)]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new CancelProductionOrderCommand(id), ct);
        return Ok(result);
    }

    /// <summary>Release the QC hold on a completed run — makes the held finished goods usable (Phase 5).</summary>
    [HttpPost("{id:long}/release-qc-hold")]
    [HasPermission(Permissions.Production.ReceiveFinishedGoods)]
    public async Task<IActionResult> ReleaseQcHold(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new ReleaseProductionQcHoldCommand(id), ct);
        return Ok(result);
    }

    /// <summary>Record the manual cost-sheet components (labour/machine/overhead/…) — Phase 6.</summary>
    [HttpPost("{id:long}/costs")]
    [HasPermission(Permissions.Production.Edit)]
    public async Task<IActionResult> UpdateCosts(long id, [FromBody] UpdateProductionCostsRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateProductionCostsCommand(
            id, request.LabourCost, request.MachineCost, request.OverheadCost,
            request.SubcontractCost, request.WastageCost, request.RejectCost), ct);
        return Ok(result);
    }

    // ── Routing stage workflow ────────────────────────────────────────────────

    [HttpPost("stages/{stageId:long}/start")]
    [HasPermission(Permissions.Production.ManageStages)]
    public async Task<IActionResult> StartStage(long stageId, CancellationToken ct)
    {
        var result = await _mediator.Send(new StartProductionStageCommand(stageId), ct);
        return Ok(result);
    }

    [HttpPost("stages/{stageId:long}/complete")]
    [HasPermission(Permissions.Production.ManageStages)]
    public async Task<IActionResult> CompleteStage(long stageId, [FromBody] CompleteProductionStageRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CompleteProductionStageCommand(
            stageId, request.CompletedQuantity, request.RejectedQuantity, request.Notes), ct);
        return Ok(result);
    }

    [HttpPost("stages/{stageId:long}/skip")]
    [HasPermission(Permissions.Production.ManageStages)]
    public async Task<IActionResult> SkipStage(long stageId, CancellationToken ct)
    {
        var result = await _mediator.Send(new SkipProductionStageCommand(stageId), ct);
        return Ok(result);
    }
}

public record CompleteProductionStageRequest(
    decimal CompletedQuantity,
    decimal RejectedQuantity,
    string? Notes);

public record UpdateProductionCostsRequest(
    decimal LabourCost,
    decimal MachineCost,
    decimal OverheadCost,
    decimal SubcontractCost,
    decimal WastageCost,
    decimal RejectCost);

public record CreateProductionOrderRequest(
    int ProductId,
    int BomId,
    decimal Quantity,
    int IssueWarehouseId,
    int ReceiveWarehouseId,
    DateOnly? PlannedStartDate,
    DateOnly? PlannedEndDate,
    string? Notes,
    IReadOnlyList<ProductionStageInput>? Stages = null,
    long? SalesOrderId = null,
    long? SalesOrderLineId = null,
    bool RequiresQc = false);

public record UpdateProductionOrderRequest(
    int ProductId,
    int BomId,
    decimal Quantity,
    int IssueWarehouseId,
    int ReceiveWarehouseId,
    DateOnly? PlannedStartDate,
    DateOnly? PlannedEndDate,
    string? Notes,
    IReadOnlyList<ProductionStageInput>? Stages = null,
    long? SalesOrderId = null,
    long? SalesOrderLineId = null,
    bool RequiresQc = false);
