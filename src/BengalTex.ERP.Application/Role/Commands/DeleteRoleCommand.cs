using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.Role.Commands;

public sealed record DeleteRoleCommand(Guid RoleId) : IRequest<ApiResponse>;

internal sealed class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, ApiResponse>
{
    private readonly IRoleManagementService _roles;

    public DeleteRoleCommandHandler(IRoleManagementService roles) => _roles = roles;

    public async Task<ApiResponse> Handle(DeleteRoleCommand cmd, CancellationToken cancellationToken)
    {
        var result = await _roles.DeleteRoleAsync(cmd.RoleId, cancellationToken);
        return result.Succeeded
            ? ApiResponse.Ok("Role deleted.")
            : ApiResponse.Fail(string.Join("; ", result.Errors));
    }
}
