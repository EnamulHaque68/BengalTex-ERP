using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Machine maintenance log — every scheduled or unscheduled maintenance event for a production
/// <see cref="Machine"/>. Tracks Preventive (scheduled), Corrective (breakdown), Inspection,
/// Calibration, Overhaul, Cleaning. Lifecycle: Scheduled → InProgress → Completed (or Cancelled).
/// When `IsRecurring=true`, completion can spawn the next occurrence via `IntervalDays`.
///
/// Cost tracking: ServiceCost = labour + technician fees, PartsCost = spare parts. Both BDT.
/// Total cost rolls up into per-machine maintenance history for analysis. v1 does NOT
/// auto-post to GL — finance team logs the expense separately via Expenses module if needed.
/// </summary>
public class MachineMaintenance : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;       // "MM-####"

    public int MachineId { get; set; }
    public Machine Machine { get; set; } = null!;

    public MaintenanceType Type { get; set; } = MaintenanceType.Preventive;

    /// <summary>Brief description of what was/is being done.</summary>
    public string Description { get; set; } = string.Empty;

    public DateOnly ScheduledDate { get; set; }
    public DateOnly? CompletedDate { get; set; }

    /// <summary>Downtime in hours — machine was unavailable for production.</summary>
    public decimal? DowntimeHours { get; set; }

    /// <summary>Technician (free text) — could be internal employee name or external vendor.</summary>
    public string? PerformedBy { get; set; }

    /// <summary>Optional employee FK when technician is on payroll.</summary>
    public int? PerformedByEmployeeId { get; set; }
    public Employee? PerformedByEmployee { get; set; }

    public decimal ServiceCost { get; set; }                // labour / external service
    public decimal PartsCost { get; set; }                  // spare parts replaced
    public decimal TotalCost => ServiceCost + PartsCost;    // computed; never persisted

    /// <summary>Free-text list of parts replaced ("belt, bearing, oil filter").</summary>
    public string? PartsReplaced { get; set; }

    /// <summary>Findings / outcome notes recorded on completion.</summary>
    public string? CompletionNotes { get; set; }

    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Scheduled;

    // ── Recurrence (for preventive maintenance series) ──
    public bool IsRecurring { get; set; }
    /// <summary>Days between occurrences when <see cref="IsRecurring"/> = true (e.g. 90 = quarterly).</summary>
    public int? IntervalDays { get; set; }
    /// <summary>FK to the PM-series anchor record (the FIRST occurrence). Lets us list "this PM's history".</summary>
    public long? RecurringSeriesAnchorId { get; set; }
    public MachineMaintenance? RecurringSeriesAnchor { get; set; }

    public string? Notes { get; set; }
}

public enum MaintenanceType
{
    Preventive = 1,        // scheduled PM (oil change, belt, calibration)
    Corrective = 2,        // breakdown / unplanned repair
    Inspection = 3,        // routine check w/o intervention
    Calibration = 4,
    Overhaul = 5,          // major rebuild
    Cleaning = 6
}

public enum MaintenanceStatus
{
    Scheduled = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4,
    Overdue = 5     // computed at row level when Scheduled && today > ScheduledDate; not stored
}
