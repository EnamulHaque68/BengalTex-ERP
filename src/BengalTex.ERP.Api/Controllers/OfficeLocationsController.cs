using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Attendance.Commands;
using BengalTex.ERP.Application.Attendance.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

/// <summary>
/// Admin management of office locations (multi-location geo-fences) and the employees authorized
/// to check in at each. This is what activates the geo-fence verification built into self check-in.
/// </summary>
[ApiController]
[Route("api/office-locations")]
[Authorize]
public class OfficeLocationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public OfficeLocationsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Attendance.View)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await _mediator.Send(new GetOfficeLocationsQuery(), ct));

    [HttpGet("{id:int}/employees")]
    [HasPermission(Permissions.Attendance.ManualEntry)]
    public async Task<IActionResult> Employees(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetOfficeLocationEmployeesQuery(id), ct));

    [HttpPost]
    [HasPermission(Permissions.Attendance.ManualEntry)]
    public async Task<IActionResult> Create([FromBody] CreateOfficeLocationCommand request, CancellationToken ct)
        => Ok(await _mediator.Send(request, ct));

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Attendance.ManualEntry)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateOfficeLocationBody body, CancellationToken ct)
        => Ok(await _mediator.Send(new UpdateOfficeLocationCommand(
            id, body.Name, body.Type, body.Latitude, body.Longitude, body.RadiusMeters, body.Address, body.IsActive), ct));

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.Attendance.ManualEntry)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteOfficeLocationCommand(id), ct));

    [HttpPut("{id:int}/employees")]
    [HasPermission(Permissions.Attendance.ManualEntry)]
    public async Task<IActionResult> SetEmployees(int id, [FromBody] SetOfficeLocationEmployeesBody body, CancellationToken ct)
        => Ok(await _mediator.Send(new SetOfficeLocationEmployeesCommand(id, body.EmployeeIds), ct));
}

public record UpdateOfficeLocationBody(
    string Name, string Type, double Latitude, double Longitude, double RadiusMeters, string? Address, bool IsActive);

public record SetOfficeLocationEmployeesBody(IReadOnlyList<int> EmployeeIds);
