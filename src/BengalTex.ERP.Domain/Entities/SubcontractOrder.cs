using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Subcontract (outsourced processing) order — material is issued to a subcontractor
/// (a <see cref="Supplier"/>) for a process (dyeing, printing, plating, …) and the
/// processed material is received back. Same item flows out and back (quantity may
/// shrink for wastage). Lifecycle: Draft → Issued → Received; cancellable while Draft.
///   Issue:   IssuedQuantity leaves <see cref="WarehouseId"/> (SubcontractIssueOut).
///   Receive: ReceivedQuantity returns to <see cref="WarehouseId"/> (SubcontractReceiveIn).
/// <see cref="ChargeAmount"/> is the processing fee (recorded; pay via a supplier invoice).
/// Transactional (long key).
/// </summary>
public class SubcontractOrder : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;

    public int SubcontractorId { get; set; }          // a Supplier
    public Supplier Subcontractor { get; set; } = null!;

    public DateOnly OrderDate { get; set; }
    public DateOnly? ExpectedReturnDate { get; set; }

    /// <summary>Process performed by the subcontractor, e.g. "Dyeing", "Printing".</summary>
    public string ProcessType { get; set; } = string.Empty;

    /// <summary>Warehouse material is issued FROM and received back INTO.</summary>
    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    public SubcontractStatus Status { get; set; } = SubcontractStatus.Draft;

    /// <summary>Processing fee charged by the subcontractor (base currency).</summary>
    public decimal ChargeAmount { get; set; }

    public DateTimeOffset? IssuedAt { get; set; }
    public string? IssuedBy { get; set; }
    public DateTimeOffset? ReceivedAt { get; set; }
    public string? ReceivedBy { get; set; }

    public string? Notes { get; set; }

    public ICollection<SubcontractLine> Lines { get; set; } = new List<SubcontractLine>();
}

/// <summary>
/// A polymorphic (RawMaterial XOR Product) line on a <see cref="SubcontractOrder"/>.
/// </summary>
public class SubcontractLine : BaseTransactionalEntity
{
    public long SubcontractOrderId { get; set; }
    public SubcontractOrder SubcontractOrder { get; set; } = null!;

    public int? RawMaterialId { get; set; }
    public RawMaterial? RawMaterial { get; set; }
    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    public decimal IssuedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }   // 0 until received

    public int SortOrder { get; set; }
    public string? LineNotes { get; set; }
}

public enum SubcontractStatus
{
    Draft = 1,
    Issued = 2,
    Received = 3,
    Cancelled = 4
}
