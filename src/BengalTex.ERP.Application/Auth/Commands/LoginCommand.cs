using BengalTex.ERP.Application.Auth.Models;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.Auth.Commands;

public record LoginCommand(
    string EmailOrUsername,
    string Password,
    string? RawDeviceFingerprint,
    string? UserAgent,
    string? IpAddress
) : IRequest<ApiResponse<AuthResponse>>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.EmailOrUsername).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, ApiResponse<AuthResponse>>
{
    private readonly IIdentityService _identityService;
    private readonly IJwtService _jwtService;
    private readonly IDeviceFingerprintService _fingerprintService;
    private readonly ISessionEnforcementService _sessionEnforcement;

    public LoginCommandHandler(
        IIdentityService identityService,
        IJwtService jwtService,
        IDeviceFingerprintService fingerprintService,
        ISessionEnforcementService sessionEnforcement)
    {
        _identityService = identityService;
        _jwtService = jwtService;
        _fingerprintService = fingerprintService;
        _sessionEnforcement = sessionEnforcement;
    }

    public async Task<ApiResponse<AuthResponse>> Handle(LoginCommand request, CancellationToken ct)
    {
        var result = await _identityService.ValidateCredentialsAsync(
            request.EmailOrUsername, request.Password, ct);

        if (result.IsLockedOut)
            return ApiResponse<AuthResponse>.Fail(
                "Account is locked due to multiple failed attempts. Try again in 15 minutes.");

        if (!result.Succeeded || result.User is null)
            return ApiResponse<AuthResponse>.Fail("Invalid email/username or password.");

        var user = result.User;

        if (!user.IsActive)
            return ApiResponse<AuthResponse>.Fail("Your account is inactive. Contact the administrator.");

        // Hash device fingerprint if provided
        string? deviceFingerprintHash = null;
        if (!string.IsNullOrEmpty(request.RawDeviceFingerprint))
            deviceFingerprintHash = _fingerprintService.HashFingerprint(request.RawDeviceFingerprint);

        // Generate tokens
        var tokenResult = _jwtService.GenerateTokens(
            user.UserId,
            user.UserName,
            user.Email,
            user.FullName,
            user.FactoryId,
            user.Roles,
            user.Permissions,
            deviceFingerprintHash);

        // Enforce single session — supersedes any prior session for this user
        var refreshTokenHash = _jwtService.HashRefreshToken(tokenResult.RefreshToken);
        await _sessionEnforcement.EnforceSingleSessionAsync(
            user.UserId,
            tokenResult.SessionId,
            refreshTokenHash,
            tokenResult.RefreshTokenExpiresAt,
            ct);

        return ApiResponse<AuthResponse>.Ok(BuildResponse(user, tokenResult), "Login successful.");
    }

    private static AuthResponse BuildResponse(UserAuthInfo user, JwtTokenResult token) =>
        new(token.AccessToken,
            token.RefreshToken,
            token.SessionId,
            token.AccessTokenExpiresAt,
            token.RefreshTokenExpiresAt,
            user.UserId.ToString(),
            user.UserName,
            user.Email,
            user.FullName,
            user.FactoryId,
            user.Roles,
            user.Permissions);
}
