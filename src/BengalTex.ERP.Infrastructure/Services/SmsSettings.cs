namespace BengalTex.ERP.Infrastructure.Services;

public class SmsSettings
{
    public string Provider { get; set; } = "DevLogger";   // DevLogger | SslWireless | Twilio
    public string? ApiKey { get; set; }
    public string? SenderId { get; set; }
    public string? BaseUrl { get; set; }
}
