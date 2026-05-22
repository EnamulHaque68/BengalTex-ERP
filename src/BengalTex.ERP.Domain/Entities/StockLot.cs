using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// A traceable lot/batch of stock — born when a <see cref="GoodsReceiptNote"/> line carrying
/// a lot number is posted. Captures the supplier batch reference, shade/colour (garments dyeing
/// consistency), and manufacture/expiry dates (chemicals, adhesives). Each posted GRN line that
/// names a lot creates one StockLot and tags its <see cref="StockMovement"/> with the lot id, so
/// inbound stock is traceable back to its physical batch.
///
/// Polymorphic item: exactly one of <see cref="RawMaterialId"/> / <see cref="ProductId"/> is set
/// (DB check constraint). In v1 lots are RM-only (GRN producer); the Product side is reserved.
///
/// <see cref="CurrentQuantity"/> starts equal to <see cref="InitialQuantity"/> and is decremented
/// by lot-aware outbound flows (consumer-side lot selection — a documented follow-up). StockLot is
/// an ADDITIVE traceability layer: it never replaces the <see cref="StockOnHand"/> total snapshot.
/// </summary>
public class StockLot : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;

    /// <summary>Supplier's / physical batch reference printed on the goods.</summary>
    public string LotNumber { get; set; } = string.Empty;

    // ── Polymorphic item — exactly one of these is non-null (DB check constraint) ──
    public int? RawMaterialId { get; set; }
    public RawMaterial? RawMaterial { get; set; }

    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    /// <summary>Source supplier for an RM lot (the GRN's PO supplier). Null for non-purchased lots.</summary>
    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    /// <summary>Shade / colour reference — critical for dyed-yarn / fabric shade matching.</summary>
    public string? Shade { get; set; }

    public DateOnly ReceivedDate { get; set; }
    public DateOnly? ManufactureDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }

    /// <summary>Quantity received into this lot (immutable once created).</summary>
    public decimal InitialQuantity { get; set; }

    /// <summary>Remaining quantity in the lot — decremented by lot-aware consumption.</summary>
    public decimal CurrentQuantity { get; set; }

    public LotStatus Status { get; set; } = LotStatus.Active;

    // ── Source document that created the lot (e.g. GRN) ──
    public string? SourceType { get; set; }
    public long? SourceId { get; set; }
    public string? SourceCode { get; set; }

    public string? Notes { get; set; }
}

public enum LotStatus
{
    Active = 1,      // has remaining quantity and is usable
    Depleted = 2,    // fully consumed (CurrentQuantity == 0)
    Quarantined = 3, // held — failed QC / under investigation
    Expired = 4      // past ExpiryDate (set by maintenance / disposition)
}
