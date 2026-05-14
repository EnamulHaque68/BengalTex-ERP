using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.RawMaterial.Commands;
using BengalTex.ERP.Application.RawMaterial.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/raw-materials")]
[Authorize]
public class RawMaterialsController : ControllerBase
{
    private readonly IMediator _mediator;

    public RawMaterialsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.RawMaterials.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] string? category = null,
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetRawMaterialsQuery(parameters, category, includeInactive), ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.RawMaterials.View)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetRawMaterialByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.RawMaterials.Create)]
    public async Task<IActionResult> Create([FromBody] CreateRawMaterialRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateRawMaterialCommand(
            request.Code, request.Name, request.Specification, request.Category,
            request.UnitOfMeasureId, request.MinimumStockLevel, request.OpeningStock,
            request.StandardCost, request.PreferredSupplierId, request.Notes
        ), ct);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.RawMaterials.Edit)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRawMaterialRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateRawMaterialCommand(
            id, request.Name, request.Specification, request.Category,
            request.UnitOfMeasureId, request.MinimumStockLevel, request.OpeningStock,
            request.StandardCost, request.PreferredSupplierId, request.Notes, request.IsActive
        ), ct);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.RawMaterials.Delete)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteRawMaterialCommand(id), ct);
        return Ok(result);
    }
}

public record CreateRawMaterialRequest(
    string? Code,
    string Name,
    string? Specification,
    string Category,
    int UnitOfMeasureId,
    decimal MinimumStockLevel,
    decimal OpeningStock,
    decimal StandardCost,
    int? PreferredSupplierId,
    string? Notes);

public record UpdateRawMaterialRequest(
    string Name,
    string? Specification,
    string Category,
    int UnitOfMeasureId,
    decimal MinimumStockLevel,
    decimal OpeningStock,
    decimal StandardCost,
    int? PreferredSupplierId,
    string? Notes,
    bool IsActive);
