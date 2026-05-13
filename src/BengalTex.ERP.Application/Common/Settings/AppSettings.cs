namespace BengalTex.ERP.Application.Common.Settings;

/// <summary>
/// App-wide settings bound from the "Application" section of appsettings.json.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Frontend SPA base URL — used to build links inside outbound emails (reset password, etc.).
    /// </summary>
    public string FrontendBaseUrl { get; set; } = "http://localhost:4200";

    public string BaseCurrency { get; set; } = "BDT";
    public double DefaultGeoFenceRadiusMeters { get; set; } = 500;
    public int MaxFailedLoginAttempts { get; set; } = 5;
    public int LockoutDurationMinutes { get; set; } = 15;
}
