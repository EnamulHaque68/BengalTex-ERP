using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Shared.Common;
using MediatR;

namespace BengalTex.ERP.Application.Auth.Commands;

public record LogoutCommand(Guid UserId) : IRequest<ApiResponse>;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, ApiResponse>
{
    private readonly ISessionEnforcementService _sessionEnforcement;

    public LogoutCommandHandler(ISessionEnforcementService sessionEnforcement)
    {
        _sessionEnforcement = sessionEnforcement;
    }

    public async Task<ApiResponse> Handle(LogoutCommand request, CancellationToken ct)
    {
        await _sessionEnforcement.ClearSessionAsync(request.UserId, ct);
        return ApiResponse.Ok("Logged out successfully.");
    }
}
