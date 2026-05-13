using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.Auth.Commands;

public record ResetPasswordCommand(
    string Email,
    string Token,
    string NewPassword,
    string ConfirmPassword
) : IRequest<ApiResponse>;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");
        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
    }
}

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ApiResponse>
{
    private readonly IIdentityService _identityService;

    public ResetPasswordCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<ApiResponse> Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        var (succeeded, errors) = await _identityService.ResetPasswordAsync(
            request.Email, request.Token, request.NewPassword, ct);

        if (!succeeded)
            return ApiResponse.Fail(string.Join("; ", errors));

        return ApiResponse.Ok("Password reset successful. Please login with your new password.");
    }
}
