using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Employee.Dtos;
using BengalTex.ERP.Application.Employee.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Employee.Commands;

internal static class EmployeeLoginHelpers
{
    /// <summary>Resolve the role to grant: explicit override first, else the employee's designation access role.</summary>
    public static async Task<string?> ResolveRoleAsync(
        string? overrideRole, int? designationId, IRepository<Designation> designationRepo, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(overrideRole)) return overrideRole.Trim();
        if (designationId is int did)
        {
            var d = await designationRepo.Query().AsNoTracking().FirstOrDefaultAsync(x => x.Id == did, ct);
            if (!string.IsNullOrWhiteSpace(d?.AccessRoleName)) return d!.AccessRoleName!.Trim();
        }
        return null;
    }
}

// ════════════════ Create login for an employee ════════════════

/// <summary>
/// Creates a login (User account) for an employee and links it (Employee.UserId). The granted role
/// defaults to the employee's designation access role (override optional). This is how an employee
/// gets dashboard access per their job designation, set by an admin.
/// </summary>
public sealed record CreateEmployeeLoginCommand(int EmployeeId, string UserName, string Password, string? RoleName, string? Email = null)
    : IRequest<ApiResponse<EmployeeLoginStatusDto>>;

public sealed class CreateEmployeeLoginCommandValidator : AbstractValidator<CreateEmployeeLoginCommand>
{
    public CreateEmployeeLoginCommandValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.UserName).NotEmpty().MaximumLength(256)
            .Matches("^[A-Za-z0-9._@-]+$").WithMessage("Username may use letters, digits, dot, underscore, @ and hyphen.");
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Enter a valid email, or leave it blank to auto-generate one.");
    }
}

internal sealed class CreateEmployeeLoginCommandHandler
    : IRequestHandler<CreateEmployeeLoginCommand, ApiResponse<EmployeeLoginStatusDto>>
{
    private readonly IRepository<Domain.Entities.Employee> _employeeRepo;
    private readonly IRepository<Designation> _designationRepo;
    private readonly IUserManagementService _users;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public CreateEmployeeLoginCommandHandler(
        IRepository<Domain.Entities.Employee> employeeRepo, IRepository<Designation> designationRepo,
        IUserManagementService users, IUnitOfWork uow, IMediator mediator)
    { _employeeRepo = employeeRepo; _designationRepo = designationRepo; _users = users; _uow = uow; _mediator = mediator; }

    public async Task<ApiResponse<EmployeeLoginStatusDto>> Handle(CreateEmployeeLoginCommand cmd, CancellationToken ct)
    {
        var e = await _employeeRepo.Query().FirstOrDefaultAsync(x => x.Id == cmd.EmployeeId, ct);
        if (e is null) return ApiResponse<EmployeeLoginStatusDto>.Fail("Employee not found.");
        if (!string.IsNullOrEmpty(e.UserId))
            return ApiResponse<EmployeeLoginStatusDto>.Fail("This employee already has a login. Reset password or unlink instead.");

        var role = await EmployeeLoginHelpers.ResolveRoleAsync(cmd.RoleName, e.DesignationId, _designationRepo, ct);
        var roles = string.IsNullOrWhiteSpace(role) ? Array.Empty<string>() : new[] { role };

        // Email priority: explicit override → employee email → auto placeholder. Must be unique across users.
        var email = !string.IsNullOrWhiteSpace(cmd.Email) ? cmd.Email!.Trim()
            : !string.IsNullOrWhiteSpace(e.Email) ? e.Email!.Trim()
            : $"{cmd.UserName.Trim().ToLowerInvariant()}@bengaltex.local";

        var result = await _users.CreateUserAsync(
            new CreateUserData(cmd.UserName.Trim(), email, e.FullName, cmd.Password, null, roles), ct);
        if (!result.Succeeded || result.UserId is null)
            return ApiResponse<EmployeeLoginStatusDto>.Fail(
                result.Errors.Count > 0 ? string.Join(" ", result.Errors) : "Could not create the login account.");

        e.UserId = result.UserId.Value.ToString();
        _employeeRepo.Update(e);
        await _uow.SaveChangesAsync(ct);

        var status = await _mediator.Send(new GetEmployeeLoginStatusQuery(e.Id), ct);
        return status.Success
            ? ApiResponse<EmployeeLoginStatusDto>.Ok(status.Data!, "Login account created.")
            : status;
    }
}

// ════════════════ Reset password ════════════════

public sealed record ResetEmployeeLoginPasswordCommand(int EmployeeId, string NewPassword) : IRequest<ApiResponse<bool>>;

public sealed class ResetEmployeeLoginPasswordCommandValidator : AbstractValidator<ResetEmployeeLoginPasswordCommand>
{
    public ResetEmployeeLoginPasswordCommandValidator()
    {
        RuleFor(x => x.EmployeeId).GreaterThan(0);
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
    }
}

internal sealed class ResetEmployeeLoginPasswordCommandHandler
    : IRequestHandler<ResetEmployeeLoginPasswordCommand, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Entities.Employee> _employeeRepo;
    private readonly IUserManagementService _users;

    public ResetEmployeeLoginPasswordCommandHandler(IRepository<Domain.Entities.Employee> employeeRepo, IUserManagementService users)
    { _employeeRepo = employeeRepo; _users = users; }

    public async Task<ApiResponse<bool>> Handle(ResetEmployeeLoginPasswordCommand cmd, CancellationToken ct)
    {
        var e = await _employeeRepo.Query().AsNoTracking().FirstOrDefaultAsync(x => x.Id == cmd.EmployeeId, ct);
        if (e is null) return ApiResponse<bool>.Fail("Employee not found.");
        if (string.IsNullOrEmpty(e.UserId) || !Guid.TryParse(e.UserId, out var uid))
            return ApiResponse<bool>.Fail("This employee has no login account.");

        var res = await _users.AdminResetPasswordAsync(uid, cmd.NewPassword, ct);
        return res.Succeeded
            ? ApiResponse<bool>.Ok(true, "Password reset.")
            : ApiResponse<bool>.Fail(res.Errors.Count > 0 ? string.Join(" ", res.Errors) : "Could not reset the password.");
    }
}

// ════════════════ Set / sync access role ════════════════

/// <summary>Sets the login's role (defaults to the employee's designation access role) — keeps access in sync with job.</summary>
public sealed record SetEmployeeLoginRoleCommand(int EmployeeId, string? RoleName) : IRequest<ApiResponse<EmployeeLoginStatusDto>>;

internal sealed class SetEmployeeLoginRoleCommandHandler
    : IRequestHandler<SetEmployeeLoginRoleCommand, ApiResponse<EmployeeLoginStatusDto>>
{
    private readonly IRepository<Domain.Entities.Employee> _employeeRepo;
    private readonly IRepository<Designation> _designationRepo;
    private readonly IUserManagementService _users;
    private readonly IMediator _mediator;

    public SetEmployeeLoginRoleCommandHandler(
        IRepository<Domain.Entities.Employee> employeeRepo, IRepository<Designation> designationRepo,
        IUserManagementService users, IMediator mediator)
    { _employeeRepo = employeeRepo; _designationRepo = designationRepo; _users = users; _mediator = mediator; }

    public async Task<ApiResponse<EmployeeLoginStatusDto>> Handle(SetEmployeeLoginRoleCommand cmd, CancellationToken ct)
    {
        var e = await _employeeRepo.Query().AsNoTracking().FirstOrDefaultAsync(x => x.Id == cmd.EmployeeId, ct);
        if (e is null) return ApiResponse<EmployeeLoginStatusDto>.Fail("Employee not found.");
        if (string.IsNullOrEmpty(e.UserId) || !Guid.TryParse(e.UserId, out var uid))
            return ApiResponse<EmployeeLoginStatusDto>.Fail("This employee has no login account.");

        var role = await EmployeeLoginHelpers.ResolveRoleAsync(cmd.RoleName, e.DesignationId, _designationRepo, ct);
        var roles = string.IsNullOrWhiteSpace(role) ? Array.Empty<string>() : new[] { role };

        var res = await _users.UpdateUserRolesAsync(uid, roles, ct);
        if (!res.Succeeded)
            return ApiResponse<EmployeeLoginStatusDto>.Fail(res.Errors.Count > 0 ? string.Join(" ", res.Errors) : "Could not update access.");

        var status = await _mediator.Send(new GetEmployeeLoginStatusQuery(e.Id), ct);
        return status.Success ? ApiResponse<EmployeeLoginStatusDto>.Ok(status.Data!, "Access updated.") : status;
    }
}

// ════════════════ Unlink login ════════════════

public sealed record UnlinkEmployeeLoginCommand(int EmployeeId, bool DeactivateUser) : IRequest<ApiResponse<bool>>;

internal sealed class UnlinkEmployeeLoginCommandHandler : IRequestHandler<UnlinkEmployeeLoginCommand, ApiResponse<bool>>
{
    private readonly IRepository<Domain.Entities.Employee> _employeeRepo;
    private readonly IUserManagementService _users;
    private readonly IUnitOfWork _uow;

    public UnlinkEmployeeLoginCommandHandler(
        IRepository<Domain.Entities.Employee> employeeRepo, IUserManagementService users, IUnitOfWork uow)
    { _employeeRepo = employeeRepo; _users = users; _uow = uow; }

    public async Task<ApiResponse<bool>> Handle(UnlinkEmployeeLoginCommand cmd, CancellationToken ct)
    {
        var e = await _employeeRepo.Query().FirstOrDefaultAsync(x => x.Id == cmd.EmployeeId, ct);
        if (e is null) return ApiResponse<bool>.Fail("Employee not found.");
        if (string.IsNullOrEmpty(e.UserId)) return ApiResponse<bool>.Fail("This employee has no login account.");

        if (cmd.DeactivateUser && Guid.TryParse(e.UserId, out var uid))
            await _users.SetUserActiveAsync(uid, false, ct);

        e.UserId = null;
        _employeeRepo.Update(e);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<bool>.Ok(true, cmd.DeactivateUser ? "Login unlinked and deactivated." : "Login unlinked.");
    }
}
