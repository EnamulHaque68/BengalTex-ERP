using BengalTex.ERP.Application.Auth.Commands;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BengalTex.ERP.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public AuthController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>POST /api/auth/login — public endpoint, no JWT required</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var command = new LoginCommand(
            request.EmailOrUsername,
            request.Password,
            request.RawDeviceFingerprint,
            Request.Headers.UserAgent.ToString(),
            HttpContext.Connection.RemoteIpAddress?.ToString());

        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    /// <summary>POST /api/auth/refresh — rotate refresh token</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken ct)
    {
        var command = new RefreshTokenCommand(request.UserId, request.RefreshToken, request.SessionId);
        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    /// <summary>POST /api/auth/logout — requires valid JWT</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        if (!Guid.TryParse(_currentUser.UserId, out var userId))
            return Ok(ApiResponse.Fail("Unauthorized."));

        var result = await _mediator.Send(new LogoutCommand(userId), ct);
        return Ok(result);
    }

    /// <summary>POST /api/auth/change-password — requires valid JWT</summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var command = new ChangePasswordCommand(
            request.CurrentPassword,
            request.NewPassword,
            request.ConfirmNewPassword);

        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }

    /// <summary>POST /api/auth/forgot-password — public, triggers reset email</summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ForgotPasswordCommand(request.Email), ct);
        return Ok(result);
    }

    /// <summary>POST /api/auth/reset-password — public, completes password reset using emailed token</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ResetPasswordCommand(
            request.Email,
            request.Token,
            request.NewPassword,
            request.ConfirmPassword), ct);
        return Ok(result);
    }

    /// <summary>GET /api/auth/me — returns current user info from token claims</summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var response = ApiResponse<object>.Ok(new
        {
            userId = _currentUser.UserId,
            userName = _currentUser.UserName,
            factoryId = _currentUser.FactoryId,
            permissions = _currentUser.Permissions
        });
        return Ok(response);
    }
}

// ─── Request DTOs (thin, just for deserialization) ────────────────────────────

public record LoginRequest(
    string EmailOrUsername,
    string Password,
    string? RawDeviceFingerprint);

public record RefreshRequest(
    Guid UserId,
    string RefreshToken,
    string SessionId);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(
    string Email,
    string Token,
    string NewPassword,
    string ConfirmPassword);
