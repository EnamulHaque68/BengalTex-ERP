using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Domain.Exceptions;
using BengalTex.ERP.Domain.ValueObjects;
using BengalTex.ERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BengalTex.ERP.Infrastructure.Services;

/// <summary>
/// Validates a user's GPS location against an assigned factory's geo-fence radius.
/// Distance is computed in-memory via the GeoLocation Haversine formula
/// (no spatial DB types — kept pure C#).
///
/// Per business rule, this service never blocks attendance — it only flags
/// (returns IsInsideFence = false plus the distance for audit/alerting).
///
/// Caching: factory geo-fence data changes rarely, so we cache per-factory
/// for 5 minutes. Stale data window after an admin update is bounded by the TTL.
/// </summary>
public class GeoFenceService : IGeoFenceService
{
    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public GeoFenceService(ApplicationDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<GeoFenceResult> ValidateAsync(int factoryId, GeoLocation location, CancellationToken ct = default)
    {
        var fence = await GetFenceAsync(factoryId, ct);

        // No geo-fence configured for this factory — treat as "inside" (no validation enforced).
        // AllowedRadiusMeters = 0 signals to callers that no fence was checked.
        if (fence.Lat is null || fence.Lng is null || fence.RadiusMeters is null)
        {
            return new GeoFenceResult(
                IsInsideFence: true,
                DistanceMeters: 0,
                FactoryId: fence.Id,
                FactoryName: fence.Name,
                AllowedRadiusMeters: 0);
        }

        var factoryLocation = GeoLocation.Create(fence.Lat.Value, fence.Lng.Value);
        var distance = location.DistanceMetersTo(factoryLocation);
        var radius = fence.RadiusMeters.Value;

        return new GeoFenceResult(
            IsInsideFence: distance <= radius,
            DistanceMeters: distance,
            FactoryId: fence.Id,
            FactoryName: fence.Name,
            AllowedRadiusMeters: (int)Math.Round(radius));
    }

    public async Task<OfficeFenceResult> ValidateForEmployeeAsync(int employeeId, GeoLocation location, CancellationToken ct = default)
    {
        // The employee's authorized, active office locations (multi-location geo-fence).
        var fences = await _db.EmployeeOfficeLocations
            .Where(e => e.EmployeeId == employeeId && e.OfficeLocation.IsActive)
            .Select(e => new { e.OfficeLocationId, e.OfficeLocation.Name, e.OfficeLocation.Latitude, e.OfficeLocation.Longitude, e.OfficeLocation.RadiusMeters })
            .ToListAsync(ct);

        if (fences.Count == 0)
            return new OfficeFenceResult(HasAnyFence: false, IsInsideAnyFence: false, NearestDistanceMeters: 0, null, null, 0);

        // Nearest authorized location; inside if within ANY location's radius.
        (double Distance, int Id, string Name, double Radius)? nearest = null;
        var insideAny = false;
        foreach (var f in fences)
        {
            var dist = location.DistanceMetersTo(GeoLocation.Create(f.Latitude, f.Longitude));
            if (dist <= f.RadiusMeters) insideAny = true;
            if (nearest is null || dist < nearest.Value.Distance)
                nearest = (dist, f.OfficeLocationId, f.Name, f.RadiusMeters);
        }

        var n = nearest!.Value;
        return new OfficeFenceResult(
            HasAnyFence: true,
            IsInsideAnyFence: insideAny,
            NearestDistanceMeters: n.Distance,
            MatchedOfficeLocationId: n.Id,
            MatchedLocationName: n.Name,
            MatchedRadiusMeters: (int)Math.Round(n.Radius));
    }

    private async Task<CachedFactoryGeoFence> GetFenceAsync(int factoryId, CancellationToken ct)
    {
        var cacheKey = $"geofence:factory:{factoryId}";

        if (_cache.TryGetValue(cacheKey, out CachedFactoryGeoFence? cached) && cached is not null)
            return cached;

        var fence = await _db.Factories
            .Where(f => f.Id == factoryId)
            .Select(f => new CachedFactoryGeoFence(
                f.Id, f.Name, f.GeoFenceLat, f.GeoFenceLng, f.GeoFenceRadiusMeters))
            .FirstOrDefaultAsync(ct);

        if (fence is null)
            throw new NotFoundException(nameof(Domain.Entities.Factory), factoryId);

        _cache.Set(cacheKey, fence, CacheTtl);
        return fence;
    }

    private sealed record CachedFactoryGeoFence(
        int Id,
        string Name,
        double? Lat,
        double? Lng,
        double? RadiusMeters);
}
