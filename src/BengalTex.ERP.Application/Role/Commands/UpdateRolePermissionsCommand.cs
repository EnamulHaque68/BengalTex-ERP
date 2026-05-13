using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.Role.Commands;

public sealed record UpdateRolePermissionsCommand(
    Guid RoleId,
    IReadOnlyList<string> Permissions
) : IRequest<ApiResponse>;

public sealed class UpdateRolePermissionsCommandValidator : AbstractValidator<UpdateRolePermissionsCommand>
{
    public UpdateRolePermissionsCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.Permissions).NotNull();
    }
}

internal sealed class UpdateRolePermissionsCommandHandler
    : IRequestHandler<UpdateRolePermissionsCommand, ApiResponse>
{
    private readonly IRoleManagementService _roles;

    public UpdateRolePermissionsCommandHandler(IRoleManagementService roles) => _roles = roles;

    public async Task<ApiResponse> Handle(UpdateRolePermissionsCommand cmd, CancellationToken cancellationToken)
    {
        var result = await _roles.UpdateRolePermissionsAsync(cmd.RoleId, cmd.Permissions, cancellationToken);
        return result.Succeeded
            ? ApiResponse.Ok("Role permissions updated.")
            : ApiResponse.Fail(string.Join("; ", result.Errors));
    }
}
