using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Warehouse.Commands;
using BengalTex.ERP.Application.Warehouse.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/warehouses")]
[Authorize]
public class WarehousesController : ControllerBase
{
    private readonly IMediator _mediator;

    public WarehousesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Warehouses.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? factoryId = null,
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetWarehousesQuery(factoryId, includeInactive), ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.Warehouses.View)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetWarehouseByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Warehouses.Create)]
    public async Task<IActionResult> Create([FromBody] CreateWarehouseRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateWarehouseCommand(
            request.Code, request.Name, request.WarehouseType,
            request.Address, request.FactoryId
        ), ct);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Warehouses.Edit)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateWarehouseRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateWarehouseCommand(
            id, request.Name, request.WarehouseType, request.Address, request.IsActive
        ), ct);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.Warehouses.Delete)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteWarehouseCommand(id), ct);
        return Ok(result);
    }
}

public record CreateWarehouseRequest(
    string Code,
    string Name,
    string WarehouseType,
    string? Address,
    int FactoryId);

public record UpdateWarehouseRequest(
    string Name,
    string WarehouseType,
    string? Address,
    bool IsActive);
