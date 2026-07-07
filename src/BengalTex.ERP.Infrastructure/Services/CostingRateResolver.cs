using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Infrastructure.Services;

/// <summary>Implements <see cref="ICostingRateResolver"/> over the CostingRates table.</summary>
public sealed class CostingRateResolver : ICostingRateResolver
{
    private readonly ApplicationDbContext _db;
    public CostingRateResolver(ApplicationDbContext db) => _db = db;

    public async Task<decimal> ResolveAsync(CostingRateType type, DateOnly date, int? workCenterId = null, CancellationToken ct = default)
    {
        // Load every active, in-effect rate of this type — do NOT pre-filter by work center.
        // Pre-filtering used to drop a work-center-specific rate whenever the caller's work center
        // was null (job cards with no routing stage) or a different center, silently zeroing the
        // absorbed cost. We keep all candidates and rank them instead.
        var candidates = await _db.CostingRates.AsNoTracking()
            .Where(r => r.IsActive && r.RateType == type && r.EffectiveFrom <= date)
            .ToListAsync(ct);
        if (candidates.Count == 0) return 0m;

        // Resolution priority (best first):
        //   1. exact work-center match (only when a work center was supplied),
        //   2. the global rate (no work center),
        //   3. last resort — any active rate of this type (so a rate configured against one work
        //      center still absorbs for un-routed / other-center work instead of silently zeroing).
        // Within each tier, the latest EffectiveFrom wins.
        return candidates
            .OrderByDescending(r => workCenterId != null && r.WorkCenterId == workCenterId)  // 1 — exact WC
            .ThenByDescending(r => r.WorkCenterId == null)                                    // 2 — global
            .ThenByDescending(r => r.EffectiveFrom)                                           // 3 — latest
            .First().Rate;
    }
}
