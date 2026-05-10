namespace BengalTex.ERP.Api.Authentication;

public interface IJwtService
{
    JwtTokenResult GenerateTokens(
        Guid userId,
        string userName,
        string email,
        string fullName,
        int? factoryId,
        IEnumerable<string> roles,
        IEnumerable<string> permissions,
        string? deviceFingerprintHash);

    string HashRefreshToken(string refreshToken);
    bool ValidateRefreshToken(string rawToken, string storedHash);
}