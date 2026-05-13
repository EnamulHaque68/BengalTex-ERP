using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.User.Commands;

public sealed record UpdateUserRolesCommand(
    Guid UserId,
    IReadOnlyList<string> Roles
) : IRequest<ApiResponse>;

public sealed class UpdateUserRolesCommandValidator : AbstractValidator<UpdateUserRolesCommand>
{
    public UpdateUserRolesCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Roles).NotNull();
    }
}

internal sealed class UpdateUserRolesCommandHandler : IRequestHandler<UpdateUserRolesCommand, ApiResponse>
{
    private readonly IUserManagementService _users;

    public UpdateUserRolesCommandHandler(IUserManagementService users) => _users = users;

    public async Task<ApiResponse> Handle(UpdateUserRolesCommand cmd, CancellationToken cancellationToken)
    {
        var result = await _users.UpdateUserRolesAsync(cmd.UserId, cmd.Roles, cancellationToken);
        return result.Succeeded
            ? ApiResponse.Ok("User roles updated.")
            : ApiResponse.Fail(string.Join("; ", result.Errors));
    }
}
