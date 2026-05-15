using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Inventory.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

/// <summary>
/// Read-only endpoints for stock state. Mutations happen via business documents
/// (GoodsReceipts, StockAdjustments) or via future modules (Production, Delivery Note).
/// </summary>
[ApiController]
[Route("api/stock")]
[Authorize]
public class StockController : ControllerBase
{
    private readonly IMediator _mediator;

    public StockController(IMediator mediator) => _mediator = mediator;

    [HttpGet("on-hand")]
    [HasPermission(Permissions.Inventory.View)]
    public async Task<IActionResult> GetOnHand(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] int? warehouseId = null,
        [FromQuery] int? rawMaterialId = null,
        [FromQuery] bool belowMinimumOnly = false,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetStockOnHandQuery(parameters, warehouseId, rawMaterialId, belowMinimumOnly), ct);
        return Ok(result);
    }

    [HttpGet("movements")]
    [HasPermission(Permissions.Inventory.View)]
    public async Task<IActionResult> GetMovements(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] int? warehouseId = null,
        [FromQuery] int? rawMaterialId = null,
        [FromQuery] string? movementType = null,
        [FromQuery] string? referenceType = null,
        [FromQuery] long? referenceId = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetStockMovementsQuery(parameters, warehouseId, rawMaterialId, movementType, referenceType, referenceId), ct);
        return Ok(result);
    }
}
