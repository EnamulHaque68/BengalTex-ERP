using BengalTex.ERP.Domain.ValueObjects;

namespace BengalTex.ERP.Application.Common.Interfaces;

public interface IGeoFenceService
{
    /// <summary>
    /// Validates the given location against the assigned factory's geo-fence.
    /// Returns Valid if inside radius, Flagged with distance otherwise.
    /// Per business rule: never blocks attendance — only flags.
    /// </summary>
    Task<GeoFenceResult> ValidateAsync(int factoryId, GeoLocation location, CancellationToken ct = default);
}

public record GeoFenceResult(
    bool IsInsideFence,
    double DistanceMeters,
    int FactoryId,
    string FactoryName,
    int AllowedRadiusMeters);