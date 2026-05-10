namespace BengalTex.ERP.Api.Authentication;

public record JwtTokenResult(
    string AccessToken,
    string RefreshToken,
    string SessionId,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt);