using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.Role.Commands;

public sealed record CreateRoleCommand(string Name, string? Description) : IRequest<ApiResponse<Guid>>;

public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100)
            .Matches("^[a-zA-Z][a-zA-Z0-9_ -]*$")
            .WithMessage("Role name must start with a letter and contain letters, digits, spaces, underscore, hyphen only.");
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

internal sealed class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, ApiResponse<Guid>>
{
    private readonly IRoleManagementService _roles;

    public CreateRoleCommandHandler(IRoleManagementService roles) => _roles = roles;

    public async Task<ApiResponse<Guid>> Handle(CreateRoleCommand cmd, CancellationToken cancellationToken)
    {
        var result = await _roles.CreateRoleAsync(new CreateRoleData(cmd.Name, cmd.Description), cancellationToken);

        if (!result.Succeeded || result.RoleId is null)
            return ApiResponse<Guid>.Fail(string.Join("; ", result.Errors));

        return ApiResponse<Guid>.Ok(result.RoleId.Value, "Role created.");
    }
}
