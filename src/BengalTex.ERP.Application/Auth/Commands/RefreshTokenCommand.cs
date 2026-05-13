using BengalTex.ERP.Application.Auth.Models;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.Auth.Commands;

public record RefreshTokenCommand(
    Guid UserId,
    string RefreshToken,
    string SessionId
) : IRequest<ApiResponse<AuthResponse>>;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.RefreshToken).NotEmpty();
        RuleFor(x => x.SessionId).NotEmpty();
    }
}

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, ApiResponse<AuthResponse>>
{
    private readonly IIdentityService _identityService;
    private readonly IJwtService _jwtService;
    private readonly ISessionEnforcementService _sessionEnforcement;

    public RefreshTokenCommandHandler(
        IIdentityService identityService,
        IJwtService jwtService,
        ISessionEnforcementService sessionEnforcement)
    {
        _identityService = identityService;
        _jwtService = jwtService;
        _sessionEnforcement = sessionEnforcement;
    }

    public async Task<ApiResponse<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var user = await _identityService.ValidateRefreshTokenAsync(
            request.UserId, request.RefreshToken, request.SessionId, ct);

        if (user is null)
            return ApiResponse<AuthResponse>.Fail("Session expired or invalid. Please login again.");

        if (!user.IsActive)
            return ApiResponse<AuthResponse>.Fail("Account is inactive.");

        // Rotate the token pair — new session id, new refresh token
        var tokenResult = _jwtService.GenerateTokens(
            user.UserId,
            user.UserName,
            user.Email,
            user.FullName,
            user.FactoryId,
            user.Roles,
            user.Permissions,
            null);

        var refreshTokenHash = _jwtService.HashRefreshToken(tokenResult.RefreshToken);
        await _sessionEnforcement.EnforceSingleSessionAsync(
            user.UserId,
            tokenResult.SessionId,
            refreshTokenHash,
            tokenResult.RefreshTokenExpiresAt,
            ct);

        var response = new AuthResponse(
            tokenResult.AccessToken,
            tokenResult.RefreshToken,
            tokenResult.SessionId,
            tokenResult.AccessTokenExpiresAt,
            tokenResult.RefreshTokenExpiresAt,
            user.UserId.ToString(),
            user.UserName,
            user.Email,
            user.FullName,
            user.FactoryId,
            user.Roles,
            user.Permissions);

        return ApiResponse<AuthResponse>.Ok(response, "Token refreshed.");
    }
}
