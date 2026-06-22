using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Employee.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Employee.Queries;

/// <summary>The login-account status for an employee (linked user + roles + designation's access role).</summary>
public sealed record GetEmployeeLoginStatusQuery(int EmployeeId) : IRequest<ApiResponse<EmployeeLoginStatusDto>>;

internal sealed class GetEmployeeLoginStatusQueryHandler
    : IRequestHandler<GetEmployeeLoginStatusQuery, ApiResponse<EmployeeLoginStatusDto>>
{
    private readonly IRepository<Domain.Entities.Employee> _employeeRepo;
    private readonly IRepository<Designation> _designationRepo;
    private readonly IUserManagementService _users;

    public GetEmployeeLoginStatusQueryHandler(
        IRepository<Domain.Entities.Employee> employeeRepo, IRepository<Designation> designationRepo, IUserManagementService users)
    { _employeeRepo = employeeRepo; _designationRepo = designationRepo; _users = users; }

    public async Task<ApiResponse<EmployeeLoginStatusDto>> Handle(GetEmployeeLoginStatusQuery req, CancellationToken ct)
    {
        var e = await _employeeRepo.Query().AsNoTracking().FirstOrDefaultAsync(x => x.Id == req.EmployeeId, ct);
        if (e is null) return ApiResponse<EmployeeLoginStatusDto>.Fail("Employee not found.");

        string? designationName = null, designationRole = null;
        if (e.DesignationId is int did)
        {
            var d = await _designationRepo.Query().AsNoTracking().FirstOrDefaultAsync(x => x.Id == did, ct);
            designationName = d?.Name;
            designationRole = d?.AccessRoleName;
        }

        string? userName = null, email = null, userId = e.UserId;
        bool? userActive = null;
        IReadOnlyList<string> roles = Array.Empty<string>();

        if (!string.IsNullOrEmpty(e.UserId) && Guid.TryParse(e.UserId, out var uid))
        {
            var user = await _users.GetUserByIdAsync(uid, ct);
            if (user is not null)
            {
                userName = user.UserName; email = user.Email; userActive = user.IsActive; roles = user.Roles;
            }
            else
            {
                userId = null; // stale link (user deleted) — treat as no login
            }
        }

        var dto = new EmployeeLoginStatusDto(
            e.Id, e.Code, e.FullName,
            HasLogin: !string.IsNullOrEmpty(userId),
            UserId: userId, UserName: userName, Email: email, UserIsActive: userActive, Roles: roles,
            DesignationName: designationName, DesignationAccessRoleName: designationRole,
            SuggestedUserName: e.Code, EmployeeEmail: e.Email);

        return ApiResponse<EmployeeLoginStatusDto>.Ok(dto);
    }
}
