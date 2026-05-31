using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Payroll.Commands;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/employee-loans")]
[Authorize]
public class EmployeeLoansController : ControllerBase
{
    private readonly IMediator _mediator;
    public EmployeeLoansController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.EmployeeLoans.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] string? status = null,
        [FromQuery] int? employeeId = null,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetEmployeeLoansQuery(parameters, status, employeeId), ct));

    [HttpPost]
    [HasPermission(Permissions.EmployeeLoans.Create)]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeLoanRequest req, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateEmployeeLoanCommand(
            req.EmployeeId, req.IssuedDate, req.Principal, req.EmiAmount,
            req.TenureMonths, req.StartYearMonth, req.Notes), ct));

    [HttpPut("{id:long}")]
    [HasPermission(Permissions.EmployeeLoans.Edit)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateEmployeeLoanRequest req, CancellationToken ct)
        => Ok(await _mediator.Send(new UpdateEmployeeLoanCommand(
            id, req.EmiAmount, req.TenureMonths, req.StartYearMonth, req.Notes), ct));

    [HttpPost("{id:long}/close")]
    [HasPermission(Permissions.EmployeeLoans.Close)]
    public async Task<IActionResult> Close(long id, [FromQuery] bool cancel = false, CancellationToken ct = default)
        => Ok(await _mediator.Send(new CloseEmployeeLoanCommand(id, cancel), ct));
}

public record CreateEmployeeLoanRequest(int EmployeeId, DateOnly IssuedDate,
    decimal Principal, decimal EmiAmount, int TenureMonths, int StartYearMonth, string? Notes);

public record UpdateEmployeeLoanRequest(decimal EmiAmount, int TenureMonths, int StartYearMonth, string? Notes);
