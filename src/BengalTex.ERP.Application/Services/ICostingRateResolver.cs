using BengalTex.ERP.Domain.Entities;

namespace BengalTex.ERP.Application.Services;

/// <summary>
/// Phase A4 — resolves the applicable absorption rate for a rate type as of a date, preferring a
/// work-center-specific rate over the global one. Returns 0 when no rate is configured, so
/// absorption is naturally skipped (production posts material-only, exactly as before A4).
/// </summary>
public interface ICostingRateResolver
{
    /// <summary>Latest active rate with EffectiveFrom ≤ <paramref name="date"/>; work-center rate beats global; 0 if none.</summary>
    Task<decimal> ResolveAsync(CostingRateType type, DateOnly date, int? workCenterId = null, CancellationToken ct = default);
}
