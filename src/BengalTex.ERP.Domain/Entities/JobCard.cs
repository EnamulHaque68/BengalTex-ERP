using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// A floor-level work unit derived from a <see cref="ProductionOrder"/>. One PO can be
/// split into many job cards (per batch / per stage / per line). Each card carries a
/// printable QR code; operators scan to Start/Pause/Resume/Complete — every action is
/// captured as a <see cref="JobCardScan"/> for full traceability and cycle-time analysis.
///
/// v1: tracking-only — no stock movement (PO Complete still drives all stock impact).
/// </summary>
public class JobCard : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;     // "JC-####" (numbering series "JC")

    public long ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;

    /// <summary>Optional: tie the card to a specific routing stage on the PO.</summary>
    public long? ProductionStageId { get; set; }
    public ProductionStage? ProductionStage { get; set; }

    /// <summary>Free-text batch / bundle identifier ("Batch-A", "Bundle 12") — printed on card.</summary>
    public string? BatchNumber { get; set; }

    /// <summary>Units to be worked through this card.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Good units completed (operator can over-ride at Complete).</summary>
    public decimal CompletedQuantity { get; set; }

    /// <summary>Units rejected during this card's work.</summary>
    public decimal RejectedQuantity { get; set; }

    public int? MachineId { get; set; }
    public Machine? Machine { get; set; }

    public int? OperatorEmployeeId { get; set; }
    public Employee? OperatorEmployee { get; set; }

    public JobCardStatus Status { get; set; } = JobCardStatus.Open;

    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? LastResumedAt { get; set; }   // for cumulative active time
    public DateTimeOffset? CompletedAt { get; set; }
    public string? CompletedBy { get; set; }

    /// <summary>Cumulative active minutes (sum of all Start→Pause spans + Resume→Complete spans).</summary>
    public int? ActiveMinutes { get; set; }

    public string? Notes { get; set; }

    public ICollection<JobCardScan> Scans { get; set; } = new List<JobCardScan>();
}

/// <summary>
/// Append-only audit log of every scan event against a <see cref="JobCard"/>.
/// One row per operator action — drives cycle-time + bottleneck analysis.
/// </summary>
public class JobCardScan : BaseTransactionalEntity
{
    public long JobCardId { get; set; }
    public JobCard JobCard { get; set; } = null!;

    public JobCardScanType ScanType { get; set; }

    public DateTimeOffset ScannedAt { get; set; }

    /// <summary>The user (operator) who triggered the scan. Stored as string to match CreatedBy convention.</summary>
    public string? ScannedBy { get; set; }

    /// <summary>Optional qty captured at this scan (mainly Complete).</summary>
    public decimal? Quantity { get; set; }
    public decimal? RejectedQuantity { get; set; }

    public string? Notes { get; set; }
}

public enum JobCardStatus
{
    Open = 1,           // created, not started
    InProgress = 2,     // started, currently running
    OnHold = 3,         // paused
    Completed = 4,
    Cancelled = 5
}

public enum JobCardScanType
{
    Start = 1,
    Pause = 2,
    Resume = 3,
    Complete = 4,
    QcCheck = 5,    // mid-process spot check (no state change)
    Cancel = 6
}
