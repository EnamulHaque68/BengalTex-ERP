using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.MachineMaintenance.Commands;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/machine-maintenance")]
[Authorize]
public class MachineMaintenanceController : ControllerBase
{
    private readonly IMediator _mediator;
    public MachineMaintenanceController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.MachineMaintenance.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] string? status = null,
        [FromQuery] string? type = null,
        [FromQuery] int? machineId = null,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetMachineMaintenancesQuery(parameters, status, type, machineId, fromDate, toDate), ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.MachineMaintenance.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetMachineMaintenanceByIdQuery(id), ct));

    [HttpPost]
    [HasPermission(Permissions.MachineMaintenance.Create)]
    public async Task<IActionResult> Schedule([FromBody] ScheduleMaintenanceCommand cmd, CancellationToken ct)
        => Ok(await _mediator.Send(cmd, ct));

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.MachineMaintenance.Edit)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateMaintenanceBody body, CancellationToken ct)
        => Ok(await _mediator.Send(new UpdateMaintenanceCommand(
            id, body.Type, body.Description, body.ScheduledDate,
            body.IsRecurring, body.IntervalDays, body.Notes), ct));

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.MachineMaintenance.Delete)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteMaintenanceCommand(id), ct));

    [HttpPost("{id:long}/start")]
    [HasPermission(Permissions.MachineMaintenance.Complete)]
    public async Task<IActionResult> Start(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new StartMaintenanceCommand(id), ct));

    [HttpPost("{id:long}/complete")]
    [HasPermission(Permissions.MachineMaintenance.Complete)]
    public async Task<IActionResult> Complete(long id, [FromBody] CompleteMaintenanceBody body, CancellationToken ct)
        => Ok(await _mediator.Send(new CompleteMaintenanceCommand(
            id, body.CompletedDate, body.DowntimeHours, body.PerformedBy, body.PerformedByEmployeeId,
            body.ServiceCost, body.PartsCost, body.PartsReplaced, body.CompletionNotes), ct));

    [HttpPost("{id:long}/cancel")]
    [HasPermission(Permissions.MachineMaintenance.Complete)]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new CancelMaintenanceCommand(id), ct));
}

public record UpdateMaintenanceBody(
    string Type, string Description, DateOnly ScheduledDate,
    bool IsRecurring, int? IntervalDays, string? Notes);

public record CompleteMaintenanceBody(
    DateOnly CompletedDate, decimal? DowntimeHours,
    string? PerformedBy, int? PerformedByEmployeeId,
    decimal ServiceCost, decimal PartsCost,
    string? PartsReplaced, string? CompletionNotes);
