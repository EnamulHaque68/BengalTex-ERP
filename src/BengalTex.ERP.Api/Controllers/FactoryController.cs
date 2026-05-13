using BengalTex.ERP.Application.Factory.Commands;
using BengalTex.ERP.Application.Factory.Queries;
using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/factories")]
[Authorize]
public class FactoryController : ControllerBase
{
    private readonly IMediator _mediator;

    public FactoryController(IMediator mediator) => _mediator = mediator;

    /// <summary>GET /api/factories — list all factories</summary>
    [HttpGet]
    [HasPermission(Permissions.Factories.View)]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetFactoriesQuery(includeInactive), ct);
        return Ok(result);
    }

    /// <summary>GET /api/factories/{id} — get single factory</summary>
    [HttpGet("{id:int}")]
    [HasPermission(Permissions.Factories.View)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetFactoryByIdQuery(id), ct);
        return Ok(result);
    }

    /// <summary>POST /api/factories — create factory</summary>
    [HttpPost]
    [HasPermission(Permissions.Factories.Create)]
    public async Task<IActionResult> Create([FromBody] CreateFactoryRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateFactoryCommand(
            request.Name, request.Code, request.CompanyId,
            request.AddressLine1, request.AddressLine2, request.City,
            request.District, request.PostalCode, request.Phone,
            request.GeoFenceLat, request.GeoFenceLng, request.GeoFenceRadiusMeters
        ), ct);
        return Ok(result);
    }

    /// <summary>PUT /api/factories/{id} — update factory</summary>
    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Factories.Edit)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateFactoryRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateFactoryCommand(
            id, request.Name, request.Code,
            request.AddressLine1, request.AddressLine2, request.City,
            request.District, request.PostalCode, request.Phone, request.IsActive,
            request.GeoFenceLat, request.GeoFenceLng, request.GeoFenceRadiusMeters
        ), ct);
        return Ok(result);
    }

    /// <summary>DELETE /api/factories/{id} — soft-delete factory</summary>
    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.Factories.Delete)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteFactoryCommand(id), ct);
        return Ok(result);
    }
}

public record CreateFactoryRequest(
    string Name,
    string Code,
    int CompanyId,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string? District,
    string? PostalCode,
    string? Phone,
    double? GeoFenceLat,
    double? GeoFenceLng,
    double? GeoFenceRadiusMeters
);

public record UpdateFactoryRequest(
    string Name,
    string Code,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string? District,
    string? PostalCode,
    string? Phone,
    bool IsActive,
    double? GeoFenceLat,
    double? GeoFenceLng,
    double? GeoFenceRadiusMeters
);
