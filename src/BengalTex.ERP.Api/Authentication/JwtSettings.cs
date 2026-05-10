namespace BengalTex.ERP.Api.Authentication;

public class JwtSettings
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public int AccessTokenExpiresInMinutes { get; set; } = 15;
    public int RefreshTokenExpiresInDays { get; set; } = 7;
}