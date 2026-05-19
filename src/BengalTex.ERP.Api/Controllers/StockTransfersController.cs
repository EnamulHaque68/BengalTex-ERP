using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.StockTransfer.Commands;
using BengalTex.ERP.Application.StockTransfer.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/stock-transfers")]
[Authorize]
public class StockTransfersController : ControllerBase
{
    private readonly IMediator _mediator;

    public StockTransfersController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Inventory.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] int? sourceWarehouseId = null,
        [FromQuery] int? destinationWarehouseId = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetStockTransfersQuery(parameters, sourceWarehouseId, destinationWarehouseId, status), ct);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.Inventory.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetStockTransferByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Inventory.Transfer)]
    public async Task<IActionResult> Create([FromBody] CreateStockTransferRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateStockTransferCommand(
            request.SourceWarehouseId, request.DestinationWarehouseId,
            request.TransferDate, request.Notes, request.Lines
        ), ct);
        return Ok(result);
    }

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.Inventory.Transfer)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateStockTransferRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateStockTransferCommand(
            id, request.SourceWarehouseId, request.DestinationWarehouseId,
            request.TransferDate, request.Notes, request.Lines
        ), ct);
        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.Inventory.Transfer)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteStockTransferCommand(id), ct);
        return Ok(result);
    }

    [HttpPost("{id:long}/post")]
    [HasPermission(Permissions.Inventory.Transfer)]
    public async Task<IActionResult> Post(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new PostStockTransferCommand(id), ct);
        return Ok(result);
    }
}

public record CreateStockTransferRequest(
    int SourceWarehouseId,
    int DestinationWarehouseId,
    DateOnly TransferDate,
    string? Notes,
    IReadOnlyList<StockTransferLineInput> Lines);

public record UpdateStockTransferRequest(
    int SourceWarehouseId,
    int DestinationWarehouseId,
    DateOnly TransferDate,
    string? Notes,
    IReadOnlyList<StockTransferLineInput> Lines);
