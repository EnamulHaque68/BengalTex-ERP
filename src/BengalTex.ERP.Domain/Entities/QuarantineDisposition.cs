using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Disposition of stock sitting in a quarantine warehouse (where QC rejects were routed).
/// Resolves quarantined stock two ways, set by <see cref="DispositionType"/>:
///   - <see cref="DispositionType.Release"/>: move it back to a usable
///     <see cref="DestinationWarehouseId"/> (re-inspected / reworked → acceptable).
///     Paired QuarantineReleaseOut + QuarantineReleaseIn movements.
///   - <see cref="DispositionType.Scrap"/>: write it off entirely (truly defective).
///     Single Scrap movement out of the quarantine warehouse.
///
/// Lifecycle: Draft → Posted (immutable). No Cancel — post a counter / adjustment to
/// correct. Lines are polymorphic (RM or Product), same check-constraint pattern as
/// stock-on-hand. Transactional (long key).
/// </summary>
public class QuarantineDisposition : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;

    public DispositionType DispositionType { get; set; }

    public DateOnly DispositionDate { get; set; }

    /// <summary>Quarantine warehouse the stock is being disposed FROM.</summary>
    public int QuarantineWarehouseId { get; set; }
    public Warehouse QuarantineWarehouse { get; set; } = null!;

    /// <summary>Destination usable warehouse — set only for Release; null for Scrap.</summary>
    public int? DestinationWarehouseId { get; set; }
    public Warehouse? DestinationWarehouse { get; set; }

    public QuarantineDispositionStatus Status { get; set; } = QuarantineDispositionStatus.Draft;

    public string? Reason { get; set; }

    public DateTimeOffset? PostedAt { get; set; }
    public string? PostedBy { get; set; }

    public string? Notes { get; set; }

    public ICollection<QuarantineDispositionLine> Lines { get; set; } = new List<QuarantineDispositionLine>();
}

/// <summary>
/// A single disposed item line. Polymorphic — exactly one of <see cref="RawMaterialId"/>
/// / <see cref="ProductId"/> set (DB check constraint).
/// </summary>
public class QuarantineDispositionLine : BaseTransactionalEntity
{
    public long QuarantineDispositionId { get; set; }
    public QuarantineDisposition QuarantineDisposition { get; set; } = null!;

    public int? RawMaterialId { get; set; }
    public RawMaterial? RawMaterial { get; set; }

    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    public decimal Quantity { get; set; }

    public int SortOrder { get; set; }

    public string? LineNotes { get; set; }
}

public enum DispositionType
{
    Release = 1,            // back to a usable warehouse (re-inspected, acceptable as-is)
    Scrap = 2,              // write-off
    Rework = 3              // back to a warehouse for reprocessing (Phase 5) — same stock move as Release,
                            // recorded distinctly so reports can tell rework from a clean release
}

public enum QuarantineDispositionStatus
{
    Draft = 1,
    Posted = 2
}
