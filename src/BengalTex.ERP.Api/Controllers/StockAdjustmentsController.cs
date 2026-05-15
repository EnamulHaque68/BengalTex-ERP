using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Inventory.Commands;
using BengalTex.ERP.Application.Inventory.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/stock-adjustments")]
[Authorize]
public class StockAdjustmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public StockAdjustmentsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Inventory.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] int? warehouseId = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetStockAdjustmentsQuery(parameters, warehouseId, status), ct);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.Inventory.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetStockAdjustmentByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Inventory.Adjust)]
    public async Task<IActionResult> Create([FromBody] CreateStockAdjustmentRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateStockAdjustmentCommand(
            request.AdjustmentDate, request.WarehouseId, request.Reason,
            request.Notes, request.Lines
        ), ct);
        return Ok(result);
    }

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.Inventory.Adjust)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateStockAdjustmentRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateStockAdjustmentCommand(
            id, request.AdjustmentDate, request.WarehouseId, request.Reason,
            request.Notes, request.Lines
        ), ct);
        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.Inventory.Adjust)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteStockAdjustmentCommand(id), ct);
        return Ok(result);
    }

    [HttpPost("{id:long}/post")]
    [HasPermission(Permissions.Inventory.Adjust)]
    public async Task<IActionResult> Post(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new PostStockAdjustmentCommand(id), ct);
        return Ok(result);
    }
}

public record CreateStockAdjustmentRequest(
    DateOnly AdjustmentDate,
    int WarehouseId,
    string Reason,
    string? Notes,
    IReadOnlyList<StockAdjustmentLineInput> Lines);

public record UpdateStockAdjustmentRequest(
    DateOnly AdjustmentDate,
    int WarehouseId,
    string Reason,
    string? Notes,
    IReadOnlyList<StockAdjustmentLineInput> Lines);
