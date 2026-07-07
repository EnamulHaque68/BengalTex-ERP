using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Phase A4 — an effective-dated absorption rate used to load conversion cost (labour, machine,
/// factory overhead) into WIP at production completion. Resolution for a given date (latest
/// <see cref="EffectiveFrom"/> ≤ date within each tier): an exact work-center match first, then a
/// global rate (null <see cref="WorkCenterId"/>), and as a last resort any active rate of the type
/// — so a rate configured against one work center still absorbs for un-routed / other-center work
/// rather than silently zeroing.
///
/// No rate configured ⇒ that cost layer is not absorbed ⇒ production posts exactly as before
/// A4 (material-only). Costing policy = actual WAC + applied rates (not standard costing).
/// </summary>
public class CostingRate : BaseEntity
{
    public CostingRateType RateType { get; set; }
    public CostingRateBasis Basis { get; set; }

    /// <summary>Rate in base BDT per the basis unit (per labour-minute / per machine-hour / per output unit).</summary>
    public decimal Rate { get; set; }

    public DateOnly EffectiveFrom { get; set; }

    /// <summary>Optional — a rate scoped to one work center; null = the global fallback rate.</summary>
    public int? WorkCenterId { get; set; }
    public WorkCenter? WorkCenter { get; set; }

    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
}

public enum CostingRateType
{
    Labour = 1,        // absorbed via job-card active minutes
    MachineOH = 2,     // absorbed via machine hours
    FactoryOH = 3      // absorbed via output quantity (or prime-cost %) — factory overhead pool
}

public enum CostingRateBasis
{
    PerLabourMinute = 1,
    PerMachineHour = 2,
    PerUnit = 3        // per output unit produced
}
