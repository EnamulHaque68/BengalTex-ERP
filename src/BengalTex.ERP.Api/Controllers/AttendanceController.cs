using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Attendance.Commands;
using BengalTex.ERP.Application.Attendance.Queries;
using BengalTex.ERP.Application.Common.Interfaces;
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
    private readonly IFileStorage _files;

    public AttendanceController(IMediator mediator, IFileStorage files)
    { _mediator = mediator; _files = files; }

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
        => Ok(await _mediator.Send(new SelfCheckInCommand(request.Latitude, request.Longitude, request.Notes, request.SelfieBase64), ct));

    [HttpPost("check-out")]
    [HasPermission(Permissions.Attendance.CheckIn)]
    public async Task<IActionResult> SelfCheckOut([FromBody] SelfCheckOutRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new SelfCheckOutCommand(request.Latitude, request.Longitude, request.SelfieBase64), ct));

    [HttpPost("break-out")]
    [HasPermission(Permissions.Attendance.CheckIn)]
    public async Task<IActionResult> BreakOut(CancellationToken ct)
        => Ok(await _mediator.Send(new BreakOutCommand(), ct));

    [HttpPost("break-in")]
    [HasPermission(Permissions.Attendance.CheckIn)]
    public async Task<IActionResult> BreakIn(CancellationToken ct)
        => Ok(await _mediator.Send(new BreakInCommand(), ct));

    /// <summary>The "My Attendance" dashboard for the logged-in user (any authenticated user).</summary>
    [HttpGet("my-attendance")]
    public async Task<IActionResult> MyAttendance(CancellationToken ct)
        => Ok(await _mediator.Send(new GetMyAttendanceQuery(), ct));

    // ════════════════ Supervisor: team view, selfie review, approvals (P3) ════════════════

    /// <summary>Team attendance for the logged-in supervisor's direct reports (HR/admin see all).</summary>
    [HttpGet("team")]
    [HasPermission(Permissions.Attendance.ApproveFlagged)]
    public async Task<IActionResult> Team(
        [FromQuery] DateOnly? fromDate, [FromQuery] DateOnly? toDate,
        [FromQuery] bool onlyFlagged = false, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetTeamAttendanceQuery(fromDate, toDate, onlyFlagged), ct));

    /// <summary>Approve or reject a Pending check-in (selfie / geo / network flagged).</summary>
    [HttpPost("{id:long}/approve")]
    [HasPermission(Permissions.Attendance.ApproveFlagged)]
    public async Task<IActionResult> ApproveAttendance(long id, [FromBody] DecideAttendanceApprovalRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new DecideAttendanceApprovalCommand(id, request.Approve, request.RejectionReason), ct));

    /// <summary>Streams a check-in/out selfie. Access: the employee, their supervisor, or an org-wide reviewer.</summary>
    [HttpGet("{id:long}/selfie")]
    public async Task<IActionResult> Selfie(long id, [FromQuery] string which = "in", CancellationToken ct = default)
    {
        var res = await _mediator.Send(new GetAttendanceSelfiePathQuery(id, which), ct);
        if (!res.Success || string.IsNullOrEmpty(res.Data)) return NotFound();
        if (!await _files.ExistsAsync(res.Data, ct)) return NotFound();
        var stream = await _files.OpenReadAsync(res.Data, ct);
        return File(stream, "image/jpeg");
    }

    // ════════════════ Attendance correction requests (P3) ════════════════

    /// <summary>Employee self-service: raise an attendance add/correction request.</summary>
    [HttpPost("requests")]
    public async Task<IActionResult> SubmitRequest([FromBody] SubmitAttendanceRequestRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new SubmitAttendanceRequestCommand(
            request.RequestDate, request.RequestType, request.RequestedCheckInTime,
            request.RequestedCheckOutTime, request.RequestedStatus, request.Reason), ct));

    /// <summary>The logged-in user's own correction requests.</summary>
    [HttpGet("requests/mine")]
    public async Task<IActionResult> MyRequests(CancellationToken ct)
        => Ok(await _mediator.Send(new GetMyAttendanceRequestsQuery(), ct));

    /// <summary>Employee cancels their own pending request.</summary>
    [HttpPost("requests/{id:long}/cancel")]
    public async Task<IActionResult> CancelRequest(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new CancelAttendanceRequestCommand(id), ct));

    /// <summary>Supervisor inbox: the team's correction requests (default Pending).</summary>
    [HttpGet("requests/team")]
    [HasPermission(Permissions.Attendance.ApproveFlagged)]
    public async Task<IActionResult> TeamRequests([FromQuery] string? status = "Pending", CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetTeamAttendanceRequestsQuery(status), ct));

    /// <summary>Supervisor approves/rejects a correction request (approve applies it to the attendance row).</summary>
    [HttpPost("requests/{id:long}/decide")]
    [HasPermission(Permissions.Attendance.ApproveFlagged)]
    public async Task<IActionResult> DecideRequest(long id, [FromBody] DecideAttendanceRequestRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new DecideAttendanceRequestCommand(id, request.Approve, request.ReviewNote), ct));

    // ════════════════ Settings (admin) ════════════════

    [HttpGet("settings")]
    [HasPermission(Permissions.Attendance.ManualEntry)]
    public async Task<IActionResult> GetSettings(CancellationToken ct)
        => Ok(await _mediator.Send(new GetAttendanceSettingsQuery(), ct));

    [HttpPut("settings")]
    [HasPermission(Permissions.Attendance.ManualEntry)]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateAttendanceSettingsCommand request, CancellationToken ct)
        => Ok(await _mediator.Send(request, ct));

    // ════════════════ Reports ════════════════

    [HttpGet("reports/daily-register")]
    [HasPermission(Permissions.Attendance.View)]
    public async Task<IActionResult> DailyRegister([FromQuery] DateOnly date, CancellationToken ct)
        => Ok(await _mediator.Send(new GetAttendanceDailyRegisterQuery(date), ct));

    [HttpGet("reports/monthly-summary")]
    [HasPermission(Permissions.Attendance.View)]
    public async Task<IActionResult> MonthlySummary(
        [FromQuery] int year, [FromQuery] int month, [FromQuery] int? employeeId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetAttendanceMonthlySummaryQuery(year, month, employeeId), ct));

    [HttpGet("reports/exceptions")]
    [HasPermission(Permissions.Attendance.View)]
    public async Task<IActionResult> Exceptions(
        [FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate, [FromQuery] string type, CancellationToken ct)
        => Ok(await _mediator.Send(new GetAttendanceExceptionsQuery(fromDate, toDate, type), ct));
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

public record SelfCheckInRequest(double? Latitude, double? Longitude, string? Notes, string? SelfieBase64 = null);

public record SelfCheckOutRequest(double? Latitude, double? Longitude, string? SelfieBase64 = null);

public record DecideAttendanceApprovalRequest(bool Approve, string? RejectionReason);

public record SubmitAttendanceRequestRequest(
    DateOnly RequestDate,
    string RequestType,
    string? RequestedCheckInTime,
    string? RequestedCheckOutTime,
    string? RequestedStatus,
    string Reason);

public record DecideAttendanceRequestRequest(bool Approve, string? ReviewNote);
