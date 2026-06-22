namespace BengalTex.ERP.Application.Attendance;

/// <summary>Parsed device facts from a User-Agent string.</summary>
public readonly record struct DeviceInfo(string? DeviceType, string? Browser, string? Os);

/// <summary>
/// Tiny, dependency-free User-Agent classifier — good enough to label a check-in's
/// device type / browser / OS for the supervisor view. Not a full UA database; pure + testable.
/// </summary>
public static class UserAgentParser
{
    public static DeviceInfo Parse(string? ua)
    {
        if (string.IsNullOrWhiteSpace(ua)) return new DeviceInfo(null, null, null);
        var s = ua.ToLowerInvariant();

        // ── OS ──
        string? os =
            s.Contains("android") ? "Android" :
            (s.Contains("iphone") || s.Contains("ipad") || s.Contains("ipod")) ? "iOS" :
            s.Contains("windows") ? "Windows" :
            (s.Contains("mac os") || s.Contains("macintosh")) ? "macOS" :
            s.Contains("cros") ? "ChromeOS" :
            s.Contains("linux") ? "Linux" : null;

        // ── Device type ──
        bool isTablet = s.Contains("ipad") || (s.Contains("android") && !s.Contains("mobile"));
        bool isMobile = s.Contains("mobile") || s.Contains("iphone") || s.Contains("ipod")
                        || (s.Contains("android") && s.Contains("mobile"));
        string deviceType = isTablet ? "Tablet" : isMobile ? "Mobile" : "Desktop";

        // ── Browser (order matters — Edge/Opera/Chrome all contain "chrome"/"safari") ──
        string? browser =
            (s.Contains("edg/") || s.Contains("edga") || s.Contains("edgios")) ? "Edge" :
            (s.Contains("opr/") || s.Contains("opera")) ? "Opera" :
            s.Contains("samsungbrowser") ? "Samsung Internet" :
            s.Contains("firefox") ? "Firefox" :
            s.Contains("chrome") ? "Chrome" :
            (s.Contains("safari") && !s.Contains("chrome")) ? "Safari" : null;

        return new DeviceInfo(deviceType, browser, os);
    }
}
