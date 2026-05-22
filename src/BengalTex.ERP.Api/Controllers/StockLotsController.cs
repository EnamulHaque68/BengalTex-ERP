using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.StockLots.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/stock-lots")]
[Authorize]
public class StockLotsController : ControllerBase
{
    private readonly IMediator _mediator;

    public StockLotsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Inventory.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] string? itemType = null,
        [FromQuery] int? warehouseId = null,
        [FromQuery] int? supplierId = null,
        [FromQuery] string? status = null,
        [FromQuery] int? expiringWithinDays = null,
        [FromQuery] bool activeOnly = false,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetStockLotsQuery(parameters, itemType, warehouseId, supplierId, status, expiringWithinDays, activeOnly), ct);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.Inventory.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetStockLotByIdQuery(id), ct);
        return Ok(result);
    }
}
