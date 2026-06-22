namespace BengalTex.ERP.Infrastructure.Services;

/// <summary>
/// Config for attendance location/network intelligence (reverse-geocoding + VPN/proxy detection).
/// Defaults use free, no-key public providers (Nominatim / ip-api). All lookups are best-effort —
/// disabling any of these simply stores nulls; it never blocks attendance.
/// </summary>
public class LocationIntelligenceSettings
{
    /// <summary>Master switch. When false, no outbound calls are made at all.</summary>
    public bool Enabled { get; set; } = true;

    public bool ReverseGeocodeEnabled { get; set; } = true;
    public bool NetworkIntelligenceEnabled { get; set; } = true;

    /// <summary>OpenStreetMap Nominatim. Self-host this URL for production volume / privacy.</summary>
    public string NominatimBaseUrl { get; set; } = "https://nominatim.openstreetmap.org";

    /// <summary>ip-api.com free endpoint (HTTP only on the free tier).</summary>
    public string IpApiBaseUrl { get; set; } = "http://ip-api.com";

    /// <summary>Identifying User-Agent — required by the Nominatim usage policy.</summary>
    public string UserAgent { get; set; } = "BengalTexERP/1.0 (attendance geo-verification)";

    /// <summary>Per-lookup timeout. Kept tight so check-in never stalls on a slow provider.</summary>
    public int TimeoutSeconds { get; set; } = 4;
}
