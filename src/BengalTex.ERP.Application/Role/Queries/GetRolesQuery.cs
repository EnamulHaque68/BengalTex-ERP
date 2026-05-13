using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Role.Dtos;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.Role.Queries;

public sealed record GetRolesQuery : IRequest<ApiResponse<IReadOnlyList<RoleListItemDto>>>;

internal sealed class GetRolesQueryHandler
    : IRequestHandler<GetRolesQuery, ApiResponse<IReadOnlyList<RoleListItemDto>>>
{
    private readonly IRoleManagementService _roles;

    public GetRolesQueryHandler(IRoleManagementService roles) => _roles = roles;

    public async Task<ApiResponse<IReadOnlyList<RoleListItemDto>>> Handle(
        GetRolesQuery request, CancellationToken cancellationToken)
    {
        var result = await _roles.ListRolesAsync(cancellationToken);
        return ApiResponse<IReadOnlyList<RoleListItemDto>>.Ok(result);
    }
}
