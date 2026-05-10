using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BengalTex.ERP.Api.Authentication;

public class JwtService : IJwtService
{
    private readonly JwtSettings _settings;

    public JwtService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    public JwtTokenResult GenerateTokens(
        Guid userId,
        string userName,
        string email,
        string fullName,
        int? factoryId,
        IEnumerable<string> roles,
        IEnumerable<string> permissions,
        string? deviceFingerprintHash)
    {
        var sessionId = Guid.NewGuid().ToString();
        var now = DateTimeOffset.UtcNow;
        var accessExpiry = now.AddMinutes(_settings.AccessTokenExpiresInMinutes);
        var refreshExpiry = now.AddDays(_settings.RefreshTokenExpiresInDays);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, userName),
            new(JwtRegisteredClaimNames.Email, email),
            new("fullName", fullName),
            new("sessionId", sessionId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (factoryId.HasValue)
            claims.Add(new Claim("factoryId", factoryId.Value.ToString()));

        if (!string.IsNullOrEmpty(deviceFingerprintHash))
            claims.Add(new Claim("deviceFp", deviceFingerprintHash));

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        foreach (var permission in permissions)
            claims.Add(new Claim("permission", permission));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var accessToken = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: accessExpiry.UtcDateTime,
            signingCredentials: creds);

        var refreshToken = GenerateSecureRandomToken();

        return new JwtTokenResult(
            new JwtSecurityTokenHandler().WriteToken(accessToken),
            refreshToken,
            sessionId,
            accessExpiry,
            refreshExpiry);
    }

    public string HashRefreshToken(string refreshToken)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(refreshToken);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    public bool ValidateRefreshToken(string rawToken, string storedHash)
    {
        var hash = HashRefreshToken(rawToken);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(hash),
            Encoding.UTF8.GetBytes(storedHash));
    }

    private static string GenerateSecureRandomToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}