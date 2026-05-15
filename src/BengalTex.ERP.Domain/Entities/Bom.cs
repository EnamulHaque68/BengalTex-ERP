using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Bill of Materials — the recipe for producing a <see cref="Product"/>: which raw
/// materials, in what quantities, for a defined output batch.
///
/// Versioned: a Product can have many BOM versions; exactly one is <see cref="IsActive"/>
/// at a time (the one production uses). Lifecycle: Draft → Approved → Archived.
/// Line quantities are expressed for <see cref="OutputQuantity"/> finished units.
/// </summary>
public class Bom : BaseEntity
{
    public string Code { get; set; } = string.Empty;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    /// <summary>Auto-incremented per Product: 1, 2, 3...</summary>
    public int Version { get; set; }

    /// <summary>Optional label, e.g. "Spring 2026 buyer spec".</summary>
    public string? Name { get; set; }

    /// <summary>How many finished units this BOM's line quantities produce.</summary>
    public decimal OutputQuantity { get; set; }

    public BomStatus Status { get; set; } = BomStatus.Draft;

    /// <summary>The version production currently uses. At most one active BOM per Product.</summary>
    public bool IsActive { get; set; }

    /// <summary>Optional date this version takes effect.</summary>
    public DateOnly? EffectiveDate { get; set; }

    public DateTimeOffset? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }

    public string? Notes { get; set; }

    public ICollection<BomLine> Lines { get; set; } = new List<BomLine>();
}

/// <summary>
/// A single raw-material requirement within a <see cref="Bom"/>, expressed for the
/// parent BOM's output batch (in the raw material's own unit of measure).
/// </summary>
public class BomLine : BaseEntity
{
    public int BomId { get; set; }
    public Bom Bom { get; set; } = null!;

    public int RawMaterialId { get; set; }
    public RawMaterial RawMaterial { get; set; } = null!;

    /// <summary>Quantity required for the parent BOM's output batch.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Expected wastage as a percentage, e.g. 5.00 = 5%.</summary>
    public decimal WastagePercent { get; set; }

    /// <summary>Display order within the BOM.</summary>
    public int SortOrder { get; set; }

    public string? LineNotes { get; set; }
}

public enum BomStatus
{
    Draft = 1,
    Approved = 2,
    Archived = 3
}
