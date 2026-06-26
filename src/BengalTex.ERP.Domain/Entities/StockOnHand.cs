using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Denormalized current quantity of an item (RawMaterial or Product) in a <see cref="Warehouse"/>.
/// Updated atomically alongside each <see cref="StockMovement"/> posting. Exactly one row
/// per (item, WarehouseId) — created on first inbound, never deleted (Quantity goes to zero
/// instead).
///
/// Polymorphic item: exactly one of <see cref="RawMaterialId"/> / <see cref="ProductId"/>
/// is set per row, enforced by a DB check constraint plus two filtered unique indexes.
/// </summary>
public class StockOnHand : BaseEntity
{
    // ── Polymorphic item — exactly one of these is non-null (DB check constraint) ──
    public int? RawMaterialId { get; set; }
    public RawMaterial? RawMaterial { get; set; }

    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    /// <summary>Current quantity in this warehouse, in the item's UoM.</summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Soft-reserved quantity (Phase 2 — Inventory Reservation): the denormalized sum of all
    /// <see cref="StockReservation"/>(Active) rows for this (item × warehouse). Does NOT move
    /// physical stock — <see cref="Quantity"/> is untouched. Available = Quantity − ReservedQuantity.
    /// Updated atomically alongside each reservation create/release.
    /// </summary>
    public decimal ReservedQuantity { get; set; }
}
