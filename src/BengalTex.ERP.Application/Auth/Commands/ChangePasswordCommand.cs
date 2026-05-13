using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.Auth.Commands;

public record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword
) : IRequest<ApiResponse>;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");
        RuleFor(x => x.ConfirmNewPassword)
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
    }
}

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, ApiResponse>
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUser;

    public ChangePasswordCommandHandler(IIdentityService identityService, ICurrentUserService currentUser)
    {
        _identityService = identityService;
        _currentUser = currentUser;
    }

    public async Task<ApiResponse> Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        if (!Guid.TryParse(_currentUser.UserId, out var userId))
            return ApiResponse.Fail("Unauthorized.");

        var (succeeded, errors) = await _identityService.ChangePasswordAsync(
            userId, request.CurrentPassword, request.NewPassword, ct);

        if (!succeeded)
            return ApiResponse.Fail(string.Join("; ", errors));

        return ApiResponse.Ok("Password changed successfully.");
    }
}
