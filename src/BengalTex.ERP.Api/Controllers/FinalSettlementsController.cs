using System.Globalization;
using System.Text;
using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Payroll.Commands;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/final-settlements")]
[Authorize]
public class FinalSettlementsController : ControllerBase
{
    private readonly IMediator _mediator;
    public FinalSettlementsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Payroll.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] string? status = null,
        [FromQuery] int? employeeId = null,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetFinalSettlementsQuery(parameters, status, employeeId), ct));

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.Payroll.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetFinalSettlementByIdQuery(id), ct));

    /// <summary>Read-only preview — auto-fills the create form with prorated salary, leave encashment, gratuity and outstanding loan.</summary>
    [HttpGet("calculate")]
    [HasPermission(Permissions.Payroll.ManageSettlement)]
    public async Task<IActionResult> Calculate(
        [FromQuery] int employeeId,
        [FromQuery] DateOnly lastWorkingDate,
        CancellationToken ct)
        => Ok(await _mediator.Send(new CalculateFinalSettlementQuery(employeeId, lastWorkingDate), ct));

    [HttpPost]
    [HasPermission(Permissions.Payroll.ManageSettlement)]
    public async Task<IActionResult> Create([FromBody] CreateFinalSettlementRequest req, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateFinalSettlementCommand(
            req.EmployeeId, req.LastWorkingDate, req.SettlementDate, req.Reason,
            req.ProratedDays, req.ProratedSalary,
            req.LeaveEncashmentDays, req.LeaveEncashmentAmount,
            req.GratuityAmount, req.OtherEarnings,
            req.OutstandingLoan, req.OtherDeductions,
            req.Notes), ct));

    [HttpPost("{id:long}/approve")]
    [HasPermission(Permissions.Payroll.ManageSettlement)]
    public async Task<IActionResult> Approve(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new ApproveFinalSettlementCommand(id), ct));

    [HttpPost("{id:long}/mark-paid")]
    [HasPermission(Permissions.Payroll.ManageSettlement)]
    public async Task<IActionResult> MarkPaid(long id, [FromBody] MarkFinalSettlementPaidRequest req, CancellationToken ct)
        => Ok(await _mediator.Send(new MarkFinalSettlementPaidCommand(id, req.PaymentMethod, req.PaymentReference), ct));

    [HttpPost("{id:long}/cancel")]
    [HasPermission(Permissions.Payroll.ManageSettlement)]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
        => Ok(await _mediator.Send(new CancelFinalSettlementCommand(id), ct));
}

public record CreateFinalSettlementRequest(
    int EmployeeId,
    DateOnly LastWorkingDate,
    DateOnly SettlementDate,
    string Reason,
    decimal ProratedDays,
    decimal ProratedSalary,
    decimal LeaveEncashmentDays,
    decimal LeaveEncashmentAmount,
    decimal GratuityAmount,
    decimal OtherEarnings,
    decimal OutstandingLoan,
    decimal OtherDeductions,
    string? Notes);

public record MarkFinalSettlementPaidRequest(string PaymentMethod, string? PaymentReference);
