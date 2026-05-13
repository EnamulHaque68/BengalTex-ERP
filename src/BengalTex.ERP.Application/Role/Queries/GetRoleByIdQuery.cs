using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Role.Dtos;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.Role.Queries;

public sealed record GetRoleByIdQuery(Guid RoleId) : IRequest<ApiResponse<RoleDto>>;

internal sealed class GetRoleByIdQueryHandler : IRequestHandler<GetRoleByIdQuery, ApiResponse<RoleDto>>
{
    private readonly IRoleManagementService _roles;

    public GetRoleByIdQueryHandler(IRoleManagementService roles) => _roles = roles;

    public async Task<ApiResponse<RoleDto>> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
    {
        var role = await _roles.GetRoleByIdAsync(request.RoleId, cancellationToken);
        return role is null
            ? ApiResponse<RoleDto>.Fail("Role not found.")
            : ApiResponse<RoleDto>.Ok(role);
    }
}
