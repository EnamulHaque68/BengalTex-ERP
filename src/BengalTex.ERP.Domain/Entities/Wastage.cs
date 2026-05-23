using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>A wastage reason code (e.g. "Setup waste", "Machine fault", "Quality reject").</summary>
public class WastageReason : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Reusable waste (can be recovered / sold as scrap) vs non-reusable loss.</summary>
    public bool IsReusable { get; set; }

    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

/// <summary>
/// An actual raw-material wastage record for cost analysis / variance reporting — captures how
/// much of which material was wasted, why, and what it cost (qty × the material's weighted-average
/// cost, snapshotted at entry). ANALYTICAL ONLY: it does NOT move stock (production issue already
/// consumes the BOM wastage allowance; a physical scrap write-off uses a Stock Adjustment), so it
/// never double-counts. Optionally linked to the production order it occurred on.
/// </summary>
public class WastageEntry : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;

    public DateOnly WastageDate { get; set; }

    public long? ProductionOrderId { get; set; }
    public ProductionOrder? ProductionOrder { get; set; }

    public int RawMaterialId { get; set; }
    public RawMaterial RawMaterial { get; set; } = null!;

    public int WastageReasonId { get; set; }
    public WastageReason WastageReason { get; set; } = null!;

    public decimal Quantity { get; set; }

    /// <summary>Snapshot of the material's weighted-average cost at entry time.</summary>
    public decimal UnitCost { get; set; }

    /// <summary>Quantity × UnitCost.</summary>
    public decimal TotalCost { get; set; }

    /// <summary>Department / machine / operator the wastage is attributed to (free text).</summary>
    public string? Department { get; set; }

    public string? Notes { get; set; }
}
