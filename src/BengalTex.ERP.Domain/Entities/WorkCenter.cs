using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// A production work center / line (Cutting, Sewing, Printing, Finishing, Packing …) — the
/// capacity + costing unit for production planning (Phase 4). Production routing
/// <see cref="ProductionStage"/>s are assigned to a work center so planned load can be measured
/// against <see cref="CapacityPerDay"/>, and (Phase 6) operation cost rolled up via
/// <see cref="CostPerHour"/>. Master data (int key).
/// </summary>
public class WorkCenter : BaseEntity
{
    public string Code { get; set; } = string.Empty;     // user-defined, e.g. "CUT-01", "SEW-A"
    public string Name { get; set; } = string.Empty;

    /// <summary>e.g. "Cutting", "Sewing", "Printing", "Finishing", "Packing" — free text.</summary>
    public string? Type { get; set; }

    /// <summary>e.g. "Floor 2 / Line 3" — free text.</summary>
    public string? Location { get; set; }

    /// <summary>Rated output per working day (in the produced product's units) — used for capacity load.</summary>
    public decimal? CapacityPerDay { get; set; }

    /// <summary>Costing rate (BDT/hour) for this work center — reserved for Phase 6 production costing.</summary>
    public decimal? CostPerHour { get; set; }

    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}
