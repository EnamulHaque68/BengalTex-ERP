using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.JobCards.Commands;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/machines")]
[Authorize]
public class MachinesController : ControllerBase
{
    private readonly IMediator _mediator;
    public MachinesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Machines.View)]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetMachinesQuery(includeInactive), ct));

    [HttpPost]
    [HasPermission(Permissions.Machines.Create)]
    public async Task<IActionResult> Create([FromBody] CreateMachineRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateMachineCommand(
            request.Code, request.Name, request.MachineType, request.Location,
            request.CapacityPerHour, request.Notes), ct));

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Machines.Edit)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateMachineRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new UpdateMachineCommand(
            id, request.Name, request.MachineType, request.Location,
            request.CapacityPerHour, request.Notes, request.IsActive), ct));

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.Machines.Delete)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteMachineCommand(id), ct));
}

public record CreateMachineRequest(
    string? Code, string Name, string? MachineType, string? Location,
    decimal? CapacityPerHour, string? Notes);

public record UpdateMachineRequest(
    string Name, string? MachineType, string? Location,
    decimal? CapacityPerHour, string? Notes, bool IsActive);
