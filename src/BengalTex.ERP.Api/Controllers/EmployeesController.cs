using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Employee.Commands;
using BengalTex.ERP.Application.Employee.Queries;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/employees")]
[Authorize]
public class EmployeesController : ControllerBase
{
    private readonly IMediator _mediator;

    public EmployeesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.Employees.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PagedQueryParameters parameters,
        [FromQuery] bool includeInactive = false,
        [FromQuery] string? department = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetEmployeesQuery(parameters, includeInactive, department, status), ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [HasPermission(Permissions.Employees.View)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetEmployeeByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Employees.Create)]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateEmployeeCommand(
            request.Code, request.FullName, request.Designation, request.Department,
            request.Phone, request.Email, request.NationalId, request.Address,
            request.JoiningDate, request.DateOfBirth, request.Gender, request.EmploymentType,
            request.BasicSalary,
            request.HouseRentAllowance, request.MedicalAllowance,
            request.TransportAllowance, request.FoodAllowance,
            request.IsPfMember, request.PfRate, request.IsTaxable,
            request.Notes
        ), ct);
        return Ok(result);
    }

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.Employees.Edit)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEmployeeRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateEmployeeCommand(
            id, request.FullName, request.Designation, request.Department,
            request.Phone, request.Email, request.NationalId, request.Address,
            request.JoiningDate, request.DateOfBirth, request.Gender, request.EmploymentType,
            request.BasicSalary,
            request.HouseRentAllowance, request.MedicalAllowance,
            request.TransportAllowance, request.FoodAllowance,
            request.IsPfMember, request.PfRate, request.IsTaxable,
            request.Status, request.Notes, request.IsActive
        ), ct);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.Employees.Delete)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteEmployeeCommand(id), ct);
        return Ok(result);
    }
}

public record CreateEmployeeRequest(
    string? Code,
    string FullName,
    string? Designation,
    string? Department,
    string? Phone,
    string? Email,
    string? NationalId,
    string? Address,
    DateOnly JoiningDate,
    DateOnly? DateOfBirth,
    string Gender,
    string EmploymentType,
    decimal BasicSalary,
    decimal HouseRentAllowance,
    decimal MedicalAllowance,
    decimal TransportAllowance,
    decimal FoodAllowance,
    bool IsPfMember,
    decimal PfRate,
    bool IsTaxable,
    string? Notes);

public record UpdateEmployeeRequest(
    string FullName,
    string? Designation,
    string? Department,
    string? Phone,
    string? Email,
    string? NationalId,
    string? Address,
    DateOnly JoiningDate,
    DateOnly? DateOfBirth,
    string Gender,
    string EmploymentType,
    decimal BasicSalary,
    decimal HouseRentAllowance,
    decimal MedicalAllowance,
    decimal TransportAllowance,
    decimal FoodAllowance,
    bool IsPfMember,
    decimal PfRate,
    bool IsTaxable,
    string Status,
    string? Notes,
    bool IsActive);
