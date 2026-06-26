using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// A soft reservation (earmark) of stock against a source document (Phase 2 — Inventory
/// Reservation). v1 is created when a <see cref="ProductionOrder"/> is planned: its BOM's
/// raw materials (and semi-finished component products) are reserved in the issue warehouse,
/// so the same physical stock cannot be silently committed to two different orders.
///
/// A reservation does NOT move stock — it only bumps the denormalized
/// <see cref="StockOnHand.ReservedQuantity"/>. <c>Available = StockOnHand.Quantity − ReservedQuantity</c>.
/// On Production Complete the reservation is <see cref="ReservationStatus.Released"/> and the
/// physical stock is issued; on Cancel/Delete it is simply released.
///
/// Polymorphic item: exactly one of <see cref="RawMaterialId"/> / <see cref="ProductId"/> is set
/// (DB check constraint), mirroring <see cref="StockOnHand"/>. Transactional (long key) — high volume.
/// </summary>
public class StockReservation : BaseTransactionalEntity
{
    // ── Polymorphic item — exactly one of these is non-null (DB check constraint) ──
    public int? RawMaterialId { get; set; }
    public RawMaterial? RawMaterial { get; set; }

    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    /// <summary>Quantity earmarked (positive), in the item's UoM.</summary>
    public decimal Quantity { get; set; }

    public ReservationStatus Status { get; set; } = ReservationStatus.Active;

    /// <summary>Source document kind that owns this reservation, e.g. "ProductionOrder".</summary>
    public string ReferenceType { get; set; } = string.Empty;

    /// <summary>Source document id.</summary>
    public long ReferenceId { get; set; }

    /// <summary>Source document display code, e.g. "BTX/PRD/2026/00001".</summary>
    public string? ReferenceCode { get; set; }

    public DateTimeOffset ReservedAt { get; set; }
    public DateTimeOffset? ReleasedAt { get; set; }

    public string? Notes { get; set; }
}

public enum ReservationStatus
{
    Active = 1,     // currently holding stock
    Released = 2    // consumed (production issued) or cancelled — no longer holding
}
