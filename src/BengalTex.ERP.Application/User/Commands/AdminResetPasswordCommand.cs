using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.User.Commands;

/// <summary>
/// Admin force-reset of a user's password (no current-password challenge).
/// Distinct from the self-service forgot-password flow which emails a token.
/// </summary>
public sealed record AdminResetPasswordCommand(
    Guid UserId,
    string NewPassword,
    string ConfirmPassword
) : IRequest<ApiResponse>;

public sealed class AdminResetPasswordCommandValidator : AbstractValidator<AdminResetPasswordCommand>
{
    public AdminResetPasswordCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");
        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
    }
}

internal sealed class AdminResetPasswordCommandHandler : IRequestHandler<AdminResetPasswordCommand, ApiResponse>
{
    private readonly IUserManagementService _users;

    public AdminResetPasswordCommandHandler(IUserManagementService users) => _users = users;

    public async Task<ApiResponse> Handle(AdminResetPasswordCommand cmd, CancellationToken cancellationToken)
    {
        var result = await _users.AdminResetPasswordAsync(cmd.UserId, cmd.NewPassword, cancellationToken);
        return result.Succeeded
            ? ApiResponse.Ok("Password reset successfully.")
            : ApiResponse.Fail(string.Join("; ", result.Errors));
    }
}
