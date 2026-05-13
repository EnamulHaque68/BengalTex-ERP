namespace BengalTex.ERP.Infrastructure.Services;

public class EmailSettings
{
    public string Provider { get; set; } = "DevLogger";   // DevLogger | Smtp | SendGrid

    // SMTP fields (used when Provider = Smtp)
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string? Username { get; set; }
    public string? Password { get; set; }

    // Common
    public string FromAddress { get; set; } = "noreply@bengaltex.com";
    public string FromName { get; set; } = "Bengal TEX ERP";
}
