using BengalTex.ERP.Domain.ValueObjects;

namespace BengalTex.ERP.Application.Common.Interfaces;

public interface IGeoFenceService
{
    /// <summary>
    /// Validates the given location against the assigned factory's geo-fence (legacy single-factory).
    /// Returns Valid if inside radius, Flagged with distance otherwise.
    /// Per business rule: never blocks attendance — only flags.
    /// </summary>
    Task<GeoFenceResult> ValidateAsync(int factoryId, GeoLocation location, CancellationToken ct = default);

    /// <summary>
    /// Multi-location validation: checks the location against ALL of the employee's authorized
    /// <c>OfficeLocation</c>s and returns the nearest match. Inside if it falls within ANY
    /// authorized location's radius. <c>HasAnyFence = false</c> when the employee has no
    /// authorized locations configured (no enforcement).
    /// </summary>
    Task<OfficeFenceResult> ValidateForEmployeeAsync(int employeeId, GeoLocation location, CancellationToken ct = default);
}

public record GeoFenceResult(
    bool IsInsideFence,
    double DistanceMeters,
    int FactoryId,
    string FactoryName,
    int AllowedRadiusMeters);

public record OfficeFenceResult(
    bool HasAnyFence,
    bool IsInsideAnyFence,
    double NearestDistanceMeters,
    int? MatchedOfficeLocationId,
    string? MatchedLocationName,
    int MatchedRadiusMeters);