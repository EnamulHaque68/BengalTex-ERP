using BengalTex.ERP.Api.Authorization;
using BengalTex.ERP.Application.AuditLog.Queries;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Employee.Commands;
using BengalTex.ERP.Application.Employee.Queries;
using BengalTex.ERP.Application.Services;
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
    private readonly IQrCodeService _qr;
    private readonly IFileStorage _files;

    public EmployeesController(IMediator mediator, IQrCodeService qr, IFileStorage files)
    { _mediator = mediator; _qr = qr; _files = files; }

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

    // ── Profile (HR profile page) ──
    /// <summary>The current user's own profile (self-service) — any authenticated user.</summary>
    [HttpGet("my-profile")]
    public async Task<IActionResult> MyProfile(CancellationToken ct)
        => Ok(await _mediator.Send(new GetMyProfileQuery(), ct));

    [HttpGet("{id:int}/profile")]
    [HasPermission(Permissions.Employees.View)]
    public async Task<IActionResult> Profile(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetEmployeeProfileQuery(id), ct));

    [HttpPut("{id:int}/profile")]
    [HasPermission(Permissions.Employees.Edit)]
    public async Task<IActionResult> UpdateProfile(int id, [FromBody] UpdateEmployeeProfileCommand command, CancellationToken ct)
    {
        if (id != command.EmployeeId) return BadRequest("Route id and body id do not match.");
        return Ok(await _mediator.Send(command, ct));
    }

    // ── Skills (profile) ──
    [HttpGet("{id:int}/skills")]
    [HasPermission(Permissions.Employees.View)]
    public async Task<IActionResult> GetSkills(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetEmployeeSkillsQuery(id), ct));

    [HttpPost("{id:int}/skills")]
    [HasPermission(Permissions.Employees.Edit)]
    public async Task<IActionResult> AddSkill(int id, [FromBody] EmployeeSkillRequest body, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateEmployeeSkillCommand(id, body.Name, body.ProficiencyPercent), ct));

    [HttpPut("skills/{skillId:int}")]
    [HasPermission(Permissions.Employees.Edit)]
    public async Task<IActionResult> UpdateSkill(int skillId, [FromBody] EmployeeSkillRequest body, CancellationToken ct)
        => Ok(await _mediator.Send(new UpdateEmployeeSkillCommand(skillId, body.Name, body.ProficiencyPercent), ct));

    [HttpDelete("skills/{skillId:int}")]
    [HasPermission(Permissions.Employees.Edit)]
    public async Task<IActionResult> DeleteSkill(int skillId, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteEmployeeSkillCommand(skillId), ct));

    // ── Education ──
    [HttpGet("{id:int}/education")]
    [HasPermission(Permissions.Employees.View)]
    public async Task<IActionResult> GetEducation(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetEmployeeEducationQuery(id), ct));

    [HttpPost("{id:int}/education")]
    [HasPermission(Permissions.Employees.Edit)]
    public async Task<IActionResult> SaveEducation(int id, [FromBody] SaveEmployeeEducationCommand body, CancellationToken ct)
        => Ok(await _mediator.Send(body with { EmployeeId = id }, ct));

    [HttpDelete("education/{eduId:int}")]
    [HasPermission(Permissions.Employees.Edit)]
    public async Task<IActionResult> DeleteEducation(int eduId, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteEmployeeEducationCommand(eduId), ct));

    // ── Emergency contacts ──
    [HttpGet("{id:int}/contacts")]
    [HasPermission(Permissions.Employees.View)]
    public async Task<IActionResult> GetContacts(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetEmployeeContactsQuery(id), ct));

    [HttpPost("{id:int}/contacts")]
    [HasPermission(Permissions.Employees.Edit)]
    public async Task<IActionResult> SaveContact(int id, [FromBody] SaveEmployeeContactCommand body, CancellationToken ct)
        => Ok(await _mediator.Send(body with { EmployeeId = id }, ct));

    [HttpDelete("contacts/{contactId:int}")]
    [HasPermission(Permissions.Employees.Edit)]
    public async Task<IActionResult> DeleteContact(int contactId, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteEmployeeContactCommand(contactId), ct));

    // ── Activity log (this employee's change history; gated by Employees.View, not AuditLog.View) ──
    [HttpGet("{id:int}/activity")]
    [HasPermission(Permissions.Employees.View)]
    public async Task<IActionResult> Activity(int id, [FromQuery] PagedQueryParameters parameters, CancellationToken ct)
        => Ok(await _mediator.Send(new GetAuditLogQuery(parameters, "Employee", null, null, null, null, id.ToString()), ct));

    // ── ID card: QR + photo ──
    [HttpGet("{id:int}/qr")]
    [HasPermission(Permissions.Employees.View)]
    public async Task<IActionResult> Qr(int id, CancellationToken ct)
    {
        var res = await _mediator.Send(new GetEmployeeByIdQuery(id), ct);
        if (!res.Success || res.Data is null) return NotFound();
        return File(_qr.GeneratePng(res.Data.Code, 8), "image/png", $"{res.Data.Code}.png");
    }

    [HttpPost("{id:int}/photo")]
    [HasPermission(Permissions.Employees.Edit)]
    [RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> UploadPhoto(int id, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest("No file provided.");
        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only image files are allowed.");
        await using var stream = file.OpenReadStream();
        var stored = await _files.SaveAsync(stream, file.FileName, file.ContentType, "Employee", ct);
        return Ok(await _mediator.Send(new SetEmployeePhotoCommand(id, stored.StoragePath), ct));
    }

    [HttpGet("{id:int}/photo")]
    [HasPermission(Permissions.Employees.View)]
    public async Task<IActionResult> Photo(int id, CancellationToken ct)
    {
        var path = await _mediator.Send(new GetEmployeePhotoPathQuery(id), ct);
        if (string.IsNullOrEmpty(path) || !await _files.ExistsAsync(path, ct)) return NotFound();
        var stream = await _files.OpenReadAsync(path, ct);
        var ext = Path.GetExtension(path).ToLowerInvariant();
        var contentType = ext switch { ".png" => "image/png", ".webp" => "image/webp", ".gif" => "image/gif", _ => "image/jpeg" };
        return File(stream, contentType);
    }

    /// <summary>Serves the logged-in user's own avatar (self-service, no permission). 404 → frontend shows initials.</summary>
    [HttpGet("my-photo")]
    public async Task<IActionResult> MyPhoto(CancellationToken ct)
    {
        var path = await _mediator.Send(new GetMyPhotoPathQuery(), ct);
        if (string.IsNullOrEmpty(path) || !await _files.ExistsAsync(path, ct)) return NotFound();
        var stream = await _files.OpenReadAsync(path, ct);
        var ext = Path.GetExtension(path).ToLowerInvariant();
        var contentType = ext switch { ".png" => "image/png", ".webp" => "image/webp", ".gif" => "image/gif", _ => "image/jpeg" };
        return File(stream, contentType);
    }

    // ── Service record: increments / promotions / transfers / disciplinary ──
    [HttpGet("{id:int}/history")]
    [HasPermission(Permissions.Employees.View)]
    public async Task<IActionResult> GetHistory(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetEmployeeHistoryQuery(id), ct));

    [HttpPost("{id:int}/history")]
    [HasPermission(Permissions.Employees.Edit)]
    public async Task<IActionResult> AddHistory(int id, [FromBody] AddEmployeeHistoryRequest body, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateEmployeeHistoryCommand(
            id, body.Type, body.EffectiveDate, body.Title, body.FromValue, body.ToValue, body.Amount, body.Details), ct));

    [HttpDelete("history/{historyId:int}")]
    [HasPermission(Permissions.Employees.Edit)]
    public async Task<IActionResult> DeleteHistory(int historyId, CancellationToken ct)
        => Ok(await _mediator.Send(new DeleteEmployeeHistoryCommand(historyId), ct));

    // ── Login account: give an employee dashboard access per their designation ──
    [HttpGet("{id:int}/login")]
    [HasPermission(Permissions.Users.View)]
    public async Task<IActionResult> LoginStatus(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetEmployeeLoginStatusQuery(id), ct));

    [HttpPost("{id:int}/login")]
    [HasPermission(Permissions.Users.Create)]
    public async Task<IActionResult> CreateLogin(int id, [FromBody] CreateEmployeeLoginRequest body, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateEmployeeLoginCommand(id, body.UserName, body.Password, body.RoleName, body.Email), ct));

    [HttpPost("{id:int}/login/reset-password")]
    [HasPermission(Permissions.Users.Edit)]
    public async Task<IActionResult> ResetLoginPassword(int id, [FromBody] ResetEmployeeLoginPasswordRequest body, CancellationToken ct)
        => Ok(await _mediator.Send(new ResetEmployeeLoginPasswordCommand(id, body.NewPassword), ct));

    [HttpPost("{id:int}/login/role")]
    [HasPermission(Permissions.Users.ManageRoles)]
    public async Task<IActionResult> SetLoginRole(int id, [FromBody] SetEmployeeLoginRoleRequest body, CancellationToken ct)
        => Ok(await _mediator.Send(new SetEmployeeLoginRoleCommand(id, body.RoleName), ct));

    [HttpDelete("{id:int}/login")]
    [HasPermission(Permissions.Users.Edit)]
    public async Task<IActionResult> UnlinkLogin(int id, [FromQuery] bool deactivate = false, CancellationToken ct = default)
        => Ok(await _mediator.Send(new UnlinkEmployeeLoginCommand(id, deactivate), ct));

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
            request.DepartmentId, request.DesignationId, request.ShiftId, request.BankAccountId,
            request.Notes, request.ReportingToEmployeeId
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
            request.DepartmentId, request.DesignationId, request.ShiftId, request.BankAccountId,
            request.Status, request.Notes, request.IsActive, request.ReportingToEmployeeId
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

public record AddEmployeeHistoryRequest(
    string Type,
    DateOnly EffectiveDate,
    string Title,
    string? FromValue,
    string? ToValue,
    decimal? Amount,
    string? Details);

public record EmployeeSkillRequest(string Name, int ProficiencyPercent);

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
    int? DepartmentId,
    int? DesignationId,
    int? ShiftId,
    int? BankAccountId,
    string? Notes,
    int? ReportingToEmployeeId = null);

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
    int? DepartmentId,
    int? DesignationId,
    int? ShiftId,
    int? BankAccountId,
    string Status,
    string? Notes,
    bool IsActive,
    int? ReportingToEmployeeId = null);

public record CreateEmployeeLoginRequest(string UserName, string Password, string? RoleName, string? Email = null);
public record ResetEmployeeLoginPasswordRequest(string NewPassword);
public record SetEmployeeLoginRoleRequest(string? RoleName);
