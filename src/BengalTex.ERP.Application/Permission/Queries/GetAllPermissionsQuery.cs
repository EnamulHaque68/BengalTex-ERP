using BengalTex.ERP.Application.Permission.Dtos;
using BengalTex.ERP.Shared.Common;
using BengalTex.ERP.Shared.Permissions;
using MediatR;

namespace BengalTex.ERP.Application.Permission.Queries;

/// <summary>
/// Returns the full hardcoded catalog of permissions, grouped by category.
/// Backed entirely by Permissions.GetAll() — no DB access required.
/// </summary>
public sealed record GetAllPermissionsQuery : IRequest<ApiResponse<IReadOnlyList<PermissionGroupDto>>>;

internal sealed class GetAllPermissionsQueryHandler
    : IRequestHandler<GetAllPermissionsQuery, ApiResponse<IReadOnlyList<PermissionGroupDto>>>
{
    public Task<ApiResponse<IReadOnlyList<PermissionGroupDto>>> Handle(
        GetAllPermissionsQuery request, CancellationToken cancellationToken)
    {
        var groups = Permissions.GetAll()
            .Select(p =>
            {
                var dot = p.IndexOf('.');
                return dot > 0
                    ? new { Category = p[..dot], Action = p[(dot + 1)..], Key = p }
                    : new { Category = "Other",  Action = p,             Key = p };
            })
            .GroupBy(x => x.Category)
            .OrderBy(g => g.Key)
            .Select(g => new PermissionGroupDto(
                g.Key,
                g.OrderBy(p => p.Action)
                 .Select(p => new PermissionItemDto(p.Key, p.Action))
                 .ToList()
                 .AsReadOnly()))
            .ToList()
            .AsReadOnly();

        IReadOnlyList<PermissionGroupDto> result = groups;
        return Task.FromResult(ApiResponse<IReadOnlyList<PermissionGroupDto>>.Ok(result));
    }
}
