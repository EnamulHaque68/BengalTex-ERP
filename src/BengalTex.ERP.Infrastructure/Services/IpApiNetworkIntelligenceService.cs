using System.Net;
using System.Text.Json;
using BengalTex.ERP.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BengalTex.ERP.Infrastructure.Services;

/// <summary>
/// Flags location-spoofing signals for a check-in IP via ip-api.com (free, no API key):
/// proxy/VPN, datacenter hosting, plus the owning ISP. Best-effort + fail-safe (errors → null flag).
/// Private / loopback IPs short-circuit (no outbound call). This FLAGS only — it never blocks attendance.
/// </summary>
public sealed class IpApiNetworkIntelligenceService : INetworkIntelligenceService
{
    private static readonly HttpClient Http = new();
    private static readonly Dictionary<string, (NetworkInspection Result, DateTimeOffset At)> Cache = new();
    private static readonly object CacheLock = new();
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(6);

    private readonly LocationIntelligenceSettings _opt;
    private readonly ILogger<IpApiNetworkIntelligenceService> _log;

    public IpApiNetworkIntelligenceService(IOptions<LocationIntelligenceSettings> opt, ILogger<IpApiNetworkIntelligenceService> log)
    { _opt = opt.Value; _log = log; }

    public async Task<NetworkInspection> InspectAsync(string? ipAddress, CancellationToken ct = default)
    {
        var empty = new NetworkInspection(null, null, null);
        if (!_opt.Enabled || !_opt.NetworkIntelligenceEnabled) return empty;
        if (string.IsNullOrWhiteSpace(ipAddress)) return empty;
        if (IsPrivateOrLocal(ipAddress)) return new NetworkInspection(false, null, "Private/local network");

        lock (CacheLock)
        {
            if (Cache.TryGetValue(ipAddress, out var hit) && DateTimeOffset.UtcNow - hit.At < CacheTtl)
                return hit.Result;
        }

        var result = empty;
        try
        {
            var url = $"{_opt.IpApiBaseUrl.TrimEnd('/')}/json/{Uri.EscapeDataString(ipAddress)}" +
                      "?fields=status,message,proxy,hosting,mobile,isp,query";

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(_opt.TimeoutSeconds));

            await using var stream = await Http.GetStreamAsync(url, cts.Token);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cts.Token);
            var root = doc.RootElement;

            var status = root.TryGetProperty("status", out var st) ? st.GetString() : null;
            if (!string.Equals(status, "success", StringComparison.OrdinalIgnoreCase))
            {
                var msg = root.TryGetProperty("message", out var m) ? m.GetString() : null;
                result = new NetworkInspection(null, null, string.IsNullOrWhiteSpace(msg) ? "Unknown IP" : Capitalize(msg!));
            }
            else
            {
                bool proxy = root.TryGetProperty("proxy", out var p) && p.ValueKind == JsonValueKind.True;
                bool hosting = root.TryGetProperty("hosting", out var h) && h.ValueKind == JsonValueKind.True;
                string? isp = root.TryGetProperty("isp", out var i) ? i.GetString() : null;

                bool flagged = proxy || hosting;
                string? note = proxy ? "VPN / Proxy" : hosting ? "Datacenter / hosting" : null;
                result = new NetworkInspection(flagged, isp, note);
            }
        }
        catch (Exception ex)
        {
            _log.LogDebug(ex, "IP intelligence lookup failed for {Ip}", ipAddress);
        }

        lock (CacheLock) { Cache[ipAddress] = (result, DateTimeOffset.UtcNow); }
        return result;
    }

    private static string Capitalize(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];

    /// <summary>Skip the API for IPs that can never be VPN/proxy-checked (LAN / loopback / link-local).</summary>
    private static bool IsPrivateOrLocal(string ip)
    {
        if (!IPAddress.TryParse(ip, out var addr)) return true; // can't parse → don't call out
        if (IPAddress.IsLoopback(addr)) return true;
        var b = addr.GetAddressBytes();
        if (addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && b.Length == 4)
        {
            if (b[0] == 10) return true;                         // 10.0.0.0/8
            if (b[0] == 172 && b[1] >= 16 && b[1] <= 31) return true; // 172.16.0.0/12
            if (b[0] == 192 && b[1] == 168) return true;         // 192.168.0.0/16
            if (b[0] == 169 && b[1] == 254) return true;         // link-local
            if (b[0] == 127) return true;
        }
        if (addr.IsIPv6LinkLocal || addr.IsIPv6SiteLocal) return true;
        return false;
    }
}
