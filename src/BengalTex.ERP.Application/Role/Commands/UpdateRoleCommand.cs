using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.Role.Commands;

public sealed record UpdateRoleCommand(
    Guid RoleId,
    string Name,
    string? Description
) : IRequest<ApiResponse>;

public sealed class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100)
            .Matches("^[a-zA-Z][a-zA-Z0-9_ -]*$")
            .WithMessage("Role name must start with a letter and contain letters, digits, spaces, underscore, hyphen only.");
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

internal sealed class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, ApiResponse>
{
    private readonly IRoleManagementService _roles;

    public UpdateRoleCommandHandler(IRoleManagementService roles) => _roles = roles;

    public async Task<ApiResponse> Handle(UpdateRoleCommand cmd, CancellationToken cancellationToken)
    {
        var result = await _roles.UpdateRoleAsync(
            cmd.RoleId, new UpdateRoleData(cmd.Name, cmd.Description), cancellationToken);

        return result.Succeeded
            ? ApiResponse.Ok("Role updated.")
            : ApiResponse.Fail(string.Join("; ", result.Errors));
    }
}
