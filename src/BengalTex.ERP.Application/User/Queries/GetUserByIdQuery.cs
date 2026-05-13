using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.User.Dtos;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.User.Queries;

public sealed record GetUserByIdQuery(Guid UserId) : IRequest<ApiResponse<UserDto>>;

internal sealed class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, ApiResponse<UserDto>>
{
    private readonly IUserManagementService _users;

    public GetUserByIdQueryHandler(IUserManagementService users) => _users = users;

    public async Task<ApiResponse<UserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _users.GetUserByIdAsync(request.UserId, cancellationToken);
        return user is null
            ? ApiResponse<UserDto>.Fail("User not found.")
            : ApiResponse<UserDto>.Ok(user);
    }
}
