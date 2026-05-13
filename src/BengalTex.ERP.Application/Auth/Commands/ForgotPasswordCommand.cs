using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Common.Settings;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Options;

namespace BengalTex.ERP.Application.Auth.Commands;

public record ForgotPasswordCommand(string Email) : IRequest<ApiResponse>;

public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);
    }
}

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, ApiResponse>
{
    // Generic response — sent regardless of whether the email exists. Prevents
    // attackers from probing which emails are registered (user enumeration).
    private const string GenericResponse =
        "If the email is registered, a password reset link has been sent.";

    private readonly IIdentityService _identityService;
    private readonly IEmailSender _emailSender;
    private readonly AppSettings _appSettings;

    public ForgotPasswordCommandHandler(
        IIdentityService identityService,
        IEmailSender emailSender,
        IOptions<AppSettings> appSettings)
    {
        _identityService = identityService;
        _emailSender = emailSender;
        _appSettings = appSettings.Value;
    }

    public async Task<ApiResponse> Handle(ForgotPasswordCommand request, CancellationToken ct)
    {
        var tokenResult = await _identityService.GeneratePasswordResetTokenAsync(request.Email, ct);

        if (tokenResult is null)
            return ApiResponse.Ok(GenericResponse);

        // Token contains URL-unsafe characters (UrlSafe Base64 + padding) — must encode.
        var encodedToken = Uri.EscapeDataString(tokenResult.Token);
        var encodedEmail = Uri.EscapeDataString(tokenResult.Email);
        var resetUrl =
            $"{_appSettings.FrontendBaseUrl.TrimEnd('/')}/login/reset-password" +
            $"?email={encodedEmail}&token={encodedToken}";

        var htmlBody = $@"
            <p>Hello {tokenResult.FullName},</p>
            <p>You requested a password reset for your Bengal TEX ERP account.</p>
            <p>Click the link below to set a new password (valid for 24 hours):</p>
            <p><a href=""{resetUrl}"">Reset Password</a></p>
            <p>If you did not request this, you can safely ignore this email.</p>
            <p>— Bengal TEX ERP</p>";

        await _emailSender.SendAsync(
            tokenResult.Email,
            "Reset Your Bengal TEX ERP Password",
            htmlBody,
            ct);

        return ApiResponse.Ok(GenericResponse);
    }
}
