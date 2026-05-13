using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.User.Commands;

public sealed record CreateUserCommand(
    string UserName,
    string Email,
    string FullName,
    string Password,
    string ConfirmPassword,
    int? FactoryId,
    IReadOnlyList<string> Roles
) : IRequest<ApiResponse<Guid>>;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().MaximumLength(100)
            .Matches("^[a-zA-Z0-9_.-]+$")
            .WithMessage("Username may contain letters, digits, underscore, dot, hyphen only.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");
        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Passwords do not match.");
        RuleFor(x => x.Roles).NotNull();
    }
}

internal sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, ApiResponse<Guid>>
{
    private readonly IUserManagementService _users;

    public CreateUserCommandHandler(IUserManagementService users) => _users = users;

    public async Task<ApiResponse<Guid>> Handle(CreateUserCommand cmd, CancellationToken cancellationToken)
    {
        var result = await _users.CreateUserAsync(new CreateUserData(
            cmd.UserName, cmd.Email, cmd.FullName, cmd.Password, cmd.FactoryId, cmd.Roles
        ), cancellationToken);

        if (!result.Succeeded || result.UserId is null)
            return ApiResponse<Guid>.Fail(string.Join("; ", result.Errors));

        return ApiResponse<Guid>.Ok(result.UserId.Value, "User created.");
    }
}
