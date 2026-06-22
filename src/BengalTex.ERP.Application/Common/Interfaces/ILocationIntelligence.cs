namespace BengalTex.ERP.Application.Common.Interfaces;

/// <summary>
/// Resolves a GPS coordinate to a human-readable address. Implementations MUST be best-effort and
/// fail-safe (return null on any error / timeout) — attendance must never break because geocoding failed.
/// </summary>
public interface IReverseGeocodeService
{
    Task<string?> ReverseAsync(double latitude, double longitude, CancellationToken ct = default);
}

/// <summary>Result of inspecting a client IP for VPN / proxy / TOR / datacenter origin.</summary>
public sealed record NetworkInspection(bool? IsProxyVpn, string? Isp, string? Note);

/// <summary>
/// Inspects a client IP for location-spoofing signals (VPN / proxy / TOR / datacenter hosting).
/// Implementations MUST be best-effort and fail-safe (return a null-flag inspection on error).
/// This is a FLAG source only — it never blocks attendance.
/// </summary>
public interface INetworkIntelligenceService
{
    Task<NetworkInspection> InspectAsync(string? ipAddress, CancellationToken ct = default);
}
