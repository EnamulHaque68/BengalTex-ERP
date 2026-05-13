using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.UnitOfMeasure.Commands;
using BengalTex.ERP.Application.UnitOfMeasure.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/units-of-measure")]
[Authorize]
public class UnitsOfMeasureController : ControllerBase
{
    private readonly IMediator _mediator;

    public UnitsOfMeasureController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.UnitsOfMeasure.View)]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetUnitsOfMeasureQuery(includeInactive), ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.UnitsOfMeasure.View)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetUnitOfMeasureByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.UnitsOfMeasure.Create)]
    public async Task<IActionResult> Create([FromBody] CreateUnitOfMeasureRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateUnitOfMeasureCommand(
            request.Code, request.Name, request.Symbol, request.UnitType,
            request.BaseUnitId, request.ConversionFactor
        ), ct);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.UnitsOfMeasure.Edit)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUnitOfMeasureRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateUnitOfMeasureCommand(
            id, request.Name, request.Symbol, request.ConversionFactor, request.IsActive
        ), ct);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.UnitsOfMeasure.Delete)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteUnitOfMeasureCommand(id), ct);
        return Ok(result);
    }
}

public record CreateUnitOfMeasureRequest(
    string Code,
    string Name,
    string Symbol,
    string UnitType,
    int? BaseUnitId,
    decimal ConversionFactor);

public record UpdateUnitOfMeasureRequest(
    string Name,
    string Symbol,
    decimal ConversionFactor,
    bool IsActive);
