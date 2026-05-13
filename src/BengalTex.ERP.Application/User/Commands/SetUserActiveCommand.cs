using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.User.Commands;

public sealed record SetUserActiveCommand(Guid UserId, bool IsActive) : IRequest<ApiResponse>;

internal sealed class SetUserActiveCommandHandler : IRequestHandler<SetUserActiveCommand, ApiResponse>
{
    private readonly IUserManagementService _users;

    public SetUserActiveCommandHandler(IUserManagementService users) => _users = users;

    public async Task<ApiResponse> Handle(SetUserActiveCommand cmd, CancellationToken cancellationToken)
    {
        var result = await _users.SetUserActiveAsync(cmd.UserId, cmd.IsActive, cancellationToken);
        return result.Succeeded
            ? ApiResponse.Ok(cmd.IsActive ? "User activated." : "User deactivated.")
            : ApiResponse.Fail(string.Join("; ", result.Errors));
    }
}
