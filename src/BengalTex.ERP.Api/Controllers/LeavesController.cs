using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Leaves.Commands;
using BengalTex.ERP.Application.Leaves.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/leave-types")]
[Authorize]
public class LeaveTypesController : ControllerBase
{
    private readonly IMediator _mediator;
    public LeaveTypesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Leaves.View)]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetLeaveTypesQuery(includeInactive), ct));

    [HttpPost]
    [HasPermission(Permissions.Leaves.ManageTypes)]
    public async Task<IActionResult> Create([FromBody] CreateLeaveTypeRequest req, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateLeaveTypeCommand(
            req.Code, req.Name, req.IsPaid, req.AnnualEntitlement, req.MaxConsecutiveDays, req.Description), ct));

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Leaves.ManageTypes)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateLeaveTypeRequest req, CancellationToken ct)
        => Ok(await _mediator.Send(new UpdateLeaveTypeCommand(
            id, req.Name, req.IsPaid, req.AnnualEntitlement, req.MaxConsecutiveDays, req.Description, req.IsActive), ct));

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.Leaves.ManageTypes)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteLeaveTypeCommand(id), ct));
}

public record CreateLeaveTypeRequest(string Code, string Name, bool IsPaid,
    decimal AnnualEntitlement, int? MaxConsecutiveDays, string? Description);
public record UpdateLeaveTypeRequest(string Name, bool IsPaid,
    decimal AnnualEntitlement, int? MaxConsecutiveDays, string? Description, bool IsActive);


[ApiController]
[Route("api/holidays")]
[Authorize]
public class HolidaysController : ControllerBase
{
    private readonly IMediator _mediator;
    public HolidaysController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Leaves.View)]
    public async Task<IActionResult> GetAll([FromQuery] int? year = null, [FromQuery] bool includeInactive = false, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetHolidaysQuery(year, includeInactive), ct));

    [HttpPost]
    [HasPermission(Permissions.Leaves.ManageHolidays)]
    public async Task<IActionResult> Create([FromBody] CreateHolidayRequest req, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateHolidayCommand(req.Date, req.Name, req.Description), ct));

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Leaves.ManageHolidays)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateHolidayRequest req, CancellationToken ct)
        => Ok(await _mediator.Send(new UpdateHolidayCommand(id, req.Date, req.Name, req.Description, req.IsActive), ct));

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.Leaves.ManageHolidays)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteHolidayCommand(id), ct));
}

public record CreateHolidayRequest(DateOnly Date, string Name, string? Description);
public record UpdateHolidayRequest(DateOnly Date, string Name, string? Description, bool IsActive);


[ApiController]
[Route("api/leave-balances")]
[Authorize]
public class LeaveBalancesController : ControllerBase
{
    private readonly IMediator _mediator;
    public LeaveBalancesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Leaves.View)]
    public async Task<IActionResult> GetAll([FromQuery] int year, [FromQuery] int? employeeId = null, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetLeaveBalancesQuery(year, employeeId), ct));

    [HttpPost("initialize/{year:int}")]
    [HasPermission(Permissions.Leaves.ManageBalances)]
    public async Task<IActionResult> Initialize(int year, CancellationToken ct)
        => Ok(await _mediator.Send(new InitializeYearBalancesCommand(year), ct));

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Leaves.ManageBalances)]
    public async Task<IActionResult> Adjust(int id, [FromBody] AdjustLeaveBalanceRequest req, CancellationToken ct)
        => Ok(await _mediator.Send(new AdjustLeaveBalanceCommand(id, req.Entitled, req.Taken), ct));
}

public record AdjustLeaveBalanceRequest(decimal Entitled, decimal Taken);


[ApiController]
[Route("api/leaves")]
[Authorize]
public class LeavesController : ControllerBase
{
    private readonly IMediator _mediator;
    public LeavesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Leaves.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] string? status = null,
        [FromQuery] int? employeeId = null,
        [FromQuery] int? leaveTypeId = null,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetLeaveApplicationsQuery(parameters, status, employeeId, leaveTypeId, fromDate, toDate), ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.Leaves.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetLeaveApplicationByIdQuery(id), ct));

    [HttpPost]
    [HasPermission(Permissions.Leaves.Apply)]
    public async Task<IActionResult> Create([FromBody] CreateLeaveApplicationRequest req, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateLeaveApplicationCommand(
            req.EmployeeId, req.LeaveTypeId, req.FromDate, req.ToDate,
            req.Reason, req.WriteAttendance, req.Notes), ct));

    [HttpPost("{id:long}/approve")]
    [HasPermission(Permissions.Leaves.Approve)]
    public async Task<IActionResult> Approve(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new ApproveLeaveApplicationCommand(id), ct));

    [HttpPost("{id:long}/reject")]
    [HasPermission(Permissions.Leaves.Approve)]
    public async Task<IActionResult> Reject(long id, [FromBody] RejectLeaveRequest req, CancellationToken ct)
        => Ok(await _mediator.Send(new RejectLeaveApplicationCommand(id, req.RejectionReason), ct));

    [HttpPost("{id:long}/cancel")]
    [HasPermission(Permissions.Leaves.Cancel)]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new CancelLeaveApplicationCommand(id), ct));
}

public record CreateLeaveApplicationRequest(
    int EmployeeId, int LeaveTypeId,
    DateOnly FromDate, DateOnly ToDate,
    string? Reason, bool WriteAttendance, string? Notes);

public record RejectLeaveRequest(string? RejectionReason);
