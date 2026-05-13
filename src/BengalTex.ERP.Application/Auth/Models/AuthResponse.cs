namespace BengalTex.ERP.Application.Auth.Models;

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    string SessionId,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt,
    string UserId,
    string UserName,
    string Email,
    string FullName,
    int? FactoryId,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);
