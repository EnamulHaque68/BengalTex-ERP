using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.WorkCenters.Commands;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

/// <summary>
/// Work centers / production lines (Phase 4). Capacity + costing master for production planning.
/// Reuses the Machines permission family (both are production resource masters).
/// </summary>
[ApiController]
[Route("api/work-centers")]
[Authorize]
public class WorkCentersController : ControllerBase
{
    private readonly IMediator _mediator;
    public WorkCentersController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Machines.View)]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetWorkCentersQuery(includeInactive), ct));

    [HttpPost]
    [HasPermission(Permissions.Machines.Create)]
    public async Task<IActionResult> Create([FromBody] CreateWorkCenterRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateWorkCenterCommand(
            request.Code, request.Name, request.Type, request.Location,
            request.CapacityPerDay, request.CostPerHour, request.Notes), ct));

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Machines.Edit)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateWorkCenterRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new UpdateWorkCenterCommand(
            id, request.Name, request.Type, request.Location,
            request.CapacityPerDay, request.CostPerHour, request.Notes, request.IsActive), ct));

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.Machines.Delete)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteWorkCenterCommand(id), ct));
}

public record CreateWorkCenterRequest(
    string Code, string Name, string? Type, string? Location,
    decimal? CapacityPerDay, decimal? CostPerHour, string? Notes);

public record UpdateWorkCenterRequest(
    string Name, string? Type, string? Location,
    decimal? CapacityPerDay, decimal? CostPerHour, string? Notes, bool IsActive);
