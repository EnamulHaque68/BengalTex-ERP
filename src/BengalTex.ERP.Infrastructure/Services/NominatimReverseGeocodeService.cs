using System.Globalization;
using System.Text.Json;
using BengalTex.ERP.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BengalTex.ERP.Infrastructure.Services;

/// <summary>
/// Reverse-geocodes GPS → address via OpenStreetMap Nominatim (free, no API key).
/// Best-effort + fail-safe: any error / timeout returns null. Results are cached in-memory
/// (coordinates rounded to ~11 m) so repeated check-ins from the same office don't re-hit the API
/// — this also keeps us within Nominatim's ≤1 req/s usage policy.
/// </summary>
public sealed class NominatimReverseGeocodeService : IReverseGeocodeService
{
    // A single shared, long-lived HttpClient (recommended over per-call construction).
    private static readonly HttpClient Http = new();
    private static readonly Dictionary<string, (string? Address, DateTimeOffset At)> Cache = new();
    private static readonly object CacheLock = new();
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(12);

    private readonly LocationIntelligenceSettings _opt;
    private readonly ILogger<NominatimReverseGeocodeService> _log;

    public NominatimReverseGeocodeService(IOptions<LocationIntelligenceSettings> opt, ILogger<NominatimReverseGeocodeService> log)
    { _opt = opt.Value; _log = log; }

    public async Task<string?> ReverseAsync(double latitude, double longitude, CancellationToken ct = default)
    {
        if (!_opt.Enabled || !_opt.ReverseGeocodeEnabled) return null;

        var key = $"{Math.Round(latitude, 4).ToString(CultureInfo.InvariantCulture)},{Math.Round(longitude, 4).ToString(CultureInfo.InvariantCulture)}";
        lock (CacheLock)
        {
            if (Cache.TryGetValue(key, out var hit) && DateTimeOffset.UtcNow - hit.At < CacheTtl)
                return hit.Address;
        }

        string? address = null;
        try
        {
            var url = $"{_opt.NominatimBaseUrl.TrimEnd('/')}/reverse?format=jsonv2" +
                      $"&lat={latitude.ToString(CultureInfo.InvariantCulture)}" +
                      $"&lon={longitude.ToString(CultureInfo.InvariantCulture)}" +
                      "&zoom=18&addressdetails=0";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.TryAddWithoutValidation("User-Agent", _opt.UserAgent);
            req.Headers.TryAddWithoutValidation("Accept-Language", "en");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(_opt.TimeoutSeconds));

            using var resp = await Http.SendAsync(req, cts.Token);
            if (resp.IsSuccessStatusCode)
            {
                await using var stream = await resp.Content.ReadAsStreamAsync(cts.Token);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cts.Token);
                if (doc.RootElement.TryGetProperty("display_name", out var dn) && dn.ValueKind == JsonValueKind.String)
                    address = dn.GetString();
            }
        }
        catch (Exception ex)
        {
            // Never let geocoding break attendance — just log and move on.
            _log.LogDebug(ex, "Reverse geocoding failed for {Lat},{Lng}", latitude, longitude);
        }

        lock (CacheLock) { Cache[key] = (address, DateTimeOffset.UtcNow); }
        return address;
    }
}
