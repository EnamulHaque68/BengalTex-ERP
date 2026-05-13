using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.User.Commands;

public sealed record UpdateUserCommand(
    Guid UserId,
    string UserName,
    string Email,
    string FullName,
    int? FactoryId
) : IRequest<ApiResponse>;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.UserName).NotEmpty().MaximumLength(100)
            .Matches("^[a-zA-Z0-9_.-]+$")
            .WithMessage("Username may contain letters, digits, underscore, dot, hyphen only.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
    }
}

internal sealed class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, ApiResponse>
{
    private readonly IUserManagementService _users;

    public UpdateUserCommandHandler(IUserManagementService users) => _users = users;

    public async Task<ApiResponse> Handle(UpdateUserCommand cmd, CancellationToken cancellationToken)
    {
        var result = await _users.UpdateUserAsync(cmd.UserId, new UpdateUserData(
            cmd.UserName, cmd.Email, cmd.FullName, cmd.FactoryId
        ), cancellationToken);

        return result.Succeeded
            ? ApiResponse.Ok("User updated.")
            : ApiResponse.Fail(string.Join("; ", result.Errors));
    }
}
