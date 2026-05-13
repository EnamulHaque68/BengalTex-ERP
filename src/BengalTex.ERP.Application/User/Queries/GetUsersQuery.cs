using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.User.Dtos;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.User.Queries;

/// <summary>Paginated user list with optional search (matches userName, email, fullName).</summary>
public sealed record GetUsersQuery(PagedQueryParameters Parameters)
    : IRequest<ApiResponse<PagedResult<UserListItemDto>>>;

internal sealed class GetUsersQueryHandler
    : IRequestHandler<GetUsersQuery, ApiResponse<PagedResult<UserListItemDto>>>
{
    private readonly IUserManagementService _users;

    public GetUsersQueryHandler(IUserManagementService users) => _users = users;

    public async Task<ApiResponse<PagedResult<UserListItemDto>>> Handle(
        GetUsersQuery request, CancellationToken cancellationToken)
    {
        var result = await _users.ListUsersAsync(request.Parameters, cancellationToken);
        return ApiResponse<PagedResult<UserListItemDto>>.Ok(result);
    }
}
