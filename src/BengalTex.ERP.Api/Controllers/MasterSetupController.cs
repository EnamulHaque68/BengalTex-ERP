using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.MasterSetup.Commands;
using BengalTex.ERP.Shared.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/departments")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly IMediator _mediator;
    public DepartmentsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.MasterSetup.View)]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetDepartmentsQuery(includeInactive), ct));

    [HttpPost]
    [HasPermission(Permissions.MasterSetup.ManageDepartments)]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentRequest req, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateDepartmentCommand(req.Code, req.Name, req.ParentDepartmentId, req.HeadEmployeeId, req.Description), ct));

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.MasterSetup.ManageDepartments)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDepartmentRequest req, CancellationToken ct)
        => Ok(await _mediator.Send(new UpdateDepartmentCommand(id, req.Code, req.Name, req.ParentDepartmentId, req.HeadEmployeeId, req.Description, req.IsActive), ct));

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.MasterSetup.ManageDepartments)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteDepartmentCommand(id), ct));
}

public record CreateDepartmentRequest(string? Code, string Name, int? ParentDepartmentId, int? HeadEmployeeId, string? Description);
public record UpdateDepartmentRequest(string? Code, string Name, int? ParentDepartmentId, int? HeadEmployeeId, string? Description, bool IsActive);


[ApiController]
[Route("api/designations")]
[Authorize]
public class DesignationsController : ControllerBase
{
    private readonly IMediator _mediator;
    public DesignationsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.MasterSetup.View)]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetDesignationsQuery(includeInactive), ct));

    [HttpPost]
    [HasPermission(Permissions.MasterSetup.ManageDesignations)]
    public async Task<IActionResult> Create([FromBody] CreateDesignationRequest req, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateDesignationCommand(req.Code, req.Name, req.GradeLevel, req.Description), ct));

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.MasterSetup.ManageDesignations)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDesignationRequest req, CancellationToken ct)
        => Ok(await _mediator.Send(new UpdateDesignationCommand(id, req.Code, req.Name, req.GradeLevel, req.Description, req.IsActive), ct));

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.MasterSetup.ManageDesignations)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteDesignationCommand(id), ct));
}

public record CreateDesignationRequest(string? Code, string Name, int? GradeLevel, string? Description);
public record UpdateDesignationRequest(string? Code, string Name, int? GradeLevel, string? Description, bool IsActive);


[ApiController]
[Route("api/shifts")]
[Authorize]
public class ShiftsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ShiftsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.MasterSetup.View)]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetShiftsQuery(includeInactive), ct));

    [HttpPost]
    [HasPermission(Permissions.MasterSetup.ManageShifts)]
    public async Task<IActionResult> Create([FromBody] CreateShiftRequest req, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateShiftCommand(
            req.Code, req.Name, req.StartTime, req.EndTime,
            req.WeekendDayOfWeek, req.SecondWeekendDayOfWeek, req.Description), ct));

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.MasterSetup.ManageShifts)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateShiftRequest req, CancellationToken ct)
        => Ok(await _mediator.Send(new UpdateShiftCommand(
            id, req.Name, req.StartTime, req.EndTime,
            req.WeekendDayOfWeek, req.SecondWeekendDayOfWeek, req.Description, req.IsActive), ct));

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.MasterSetup.ManageShifts)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteShiftCommand(id), ct));
}

public record CreateShiftRequest(string Code, string Name, string StartTime, string EndTime,
    string WeekendDayOfWeek, string? SecondWeekendDayOfWeek, string? Description);

public record UpdateShiftRequest(string Name, string StartTime, string EndTime,
    string WeekendDayOfWeek, string? SecondWeekendDayOfWeek, string? Description, bool IsActive);


[ApiController]
[Route("api/bank-accounts")]
[Authorize]
public class BankAccountsController : ControllerBase
{
    private readonly IMediator _mediator;
    public BankAccountsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [HasPermission(Permissions.MasterSetup.View)]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetBankAccountsQuery(includeInactive), ct));

    [HttpPost]
    [HasPermission(Permissions.MasterSetup.ManageBankAccounts)]
    public async Task<IActionResult> Create([FromBody] CreateBankAccountRequest req, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateBankAccountCommand(
            req.AccountName, req.BankName, req.BranchName, req.AccountNumber, req.AccountType,
            req.RoutingNumber, req.SwiftCode, req.Currency, req.LedgerAccountId, req.Notes), ct));

    [HttpPut("{id:int}")]
    [HasPermission(Permissions.MasterSetup.ManageBankAccounts)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateBankAccountRequest req, CancellationToken ct)
        => Ok(await _mediator.Send(new UpdateBankAccountCommand(
            id, req.AccountName, req.BankName, req.BranchName, req.AccountNumber, req.AccountType,
            req.RoutingNumber, req.SwiftCode, req.Currency, req.LedgerAccountId, req.Notes, req.IsActive), ct));

    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.MasterSetup.ManageBankAccounts)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteBankAccountCommand(id), ct));
}

public record CreateBankAccountRequest(string AccountName, string BankName, string? BranchName,
    string AccountNumber, string AccountType, string? RoutingNumber, string? SwiftCode,
    string Currency, int? LedgerAccountId, string? Notes);

public record UpdateBankAccountRequest(string AccountName, string BankName, string? BranchName,
    string AccountNumber, string AccountType, string? RoutingNumber, string? SwiftCode,
    string Currency, int? LedgerAccountId, string? Notes, bool IsActive);
