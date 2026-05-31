using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Payroll.Commands;
using BengalTex.ERP.Application.Payroll.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/payroll")]
[Authorize]
public class PayrollController : ControllerBase
{
    private readonly IMediator _mediator;

    public PayrollController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Payroll.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] int? year = null,
        [FromQuery] int? month = null,
        [FromQuery] int? employeeId = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetPayslipsQuery(parameters, year, month, employeeId, status), ct);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [HasPermission(Permissions.Payroll.View)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPayslipByIdQuery(id), ct);
        return Ok(result);
    }

    /// <summary>Generate draft payslips for all active employees for a month.</summary>
    [HttpPost("generate")]
    [HasPermission(Permissions.Payroll.Process)]
    public async Task<IActionResult> Generate([FromBody] GeneratePayrollRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new GeneratePayrollCommand(request.Year, request.Month), ct);
        return Ok(result);
    }

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.Payroll.Process)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdatePayslipRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdatePayslipCommand(
            id, request.OvertimeAmount, request.Allowances, request.Deductions,
            request.HouseRent, request.Medical, request.Transport, request.FoodAllowance, request.FestivalBonus,
            request.PfEmployee, request.PfEmployer, request.IncomeTax, request.LoanDeduction,
            request.Notes), ct);
        return Ok(result);
    }

    [HttpPost("{id:long}/mark-paid")]
    [HasPermission(Permissions.Payroll.Process)]
    public async Task<IActionResult> MarkPaid(long id, [FromBody] MarkPayslipPaidRequest? request, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new MarkPayslipPaidCommand(id, request?.PaymentMethod), ct);
        return Ok(result);
    }

    [HttpDelete("{id:long}")]
    [HasPermission(Permissions.Payroll.Process)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeletePayslipCommand(id), ct);
        return Ok(result);
    }
}

public record GeneratePayrollRequest(int Year, int Month);

public record MarkPayslipPaidRequest(string? PaymentMethod);

public record UpdatePayslipRequest(
    decimal OvertimeAmount,
    decimal Allowances,
    decimal Deductions,
    decimal HouseRent,
    decimal Medical,
    decimal Transport,
    decimal FoodAllowance,
    decimal FestivalBonus,
    decimal PfEmployee,
    decimal PfEmployer,
    decimal IncomeTax,
    decimal LoanDeduction,
    string? Notes);
