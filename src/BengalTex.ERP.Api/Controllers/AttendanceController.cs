using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Attendance.Commands;
using BengalTex.ERP.Application.Attendance.Queries;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/attendance")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly IMediator _mediator;

    public AttendanceController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Attendance.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        [FromQuery] int? employeeId = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetAttendanceRecordsQuery(parameters, fromDate, toDate, employeeId, status), ct);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.Attendance.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAttendanceByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Attendance.ManualEntry)]
    public async Task<IActionResult> Create([FromBody] CreateAttendanceRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateAttendanceCommand(
            request.EmployeeId, request.AttendanceDate, request.Status,
            request.CheckInTime, request.CheckOutTime, request.OvertimeHours, request.Notes
        ), ct);
        return Ok(result);
    }

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.Attendance.ManualEntry)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateAttendanceRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateAttendanceCommand(
            id, request.Status, request.CheckInTime, request.CheckOutTime, request.OvertimeHours, request.Notes
        ), ct);
        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.Attendance.ManualEntry)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteAttendanceCommand(id), ct);
        return Ok(result);
    }

    /// <summary>
    /// Self-service check-in for the logged-in user. Optional GPS coordinates are validated
    /// against the user's factory geo-fence (out-of-fence flagged but not blocked).
    /// </summary>
    [HttpPost("check-in")]
    [HasPermission(Permissions.Attendance.CheckIn)]
    public async Task<IActionResult> SelfCheckIn([FromBody] SelfCheckInRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new SelfCheckInCommand(
            request.Latitude, request.Longitude, request.Notes), ct);
        return Ok(result);
    }
}

public record CreateAttendanceRequest(
    int EmployeeId,
    DateOnly AttendanceDate,
    string Status,
    string? CheckInTime,
    string? CheckOutTime,
    decimal OvertimeHours,
    string? Notes);

public record UpdateAttendanceRequest(
    string Status,
    string? CheckInTime,
    string? CheckOutTime,
    decimal OvertimeHours,
    string? Notes);

public record SelfCheckInRequest(double? Latitude, double? Longitude, string? Notes);
