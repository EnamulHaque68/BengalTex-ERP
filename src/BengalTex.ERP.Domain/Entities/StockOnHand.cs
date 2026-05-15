using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Denormalized current quantity of a <see cref="RawMaterial"/> in a <see cref="Warehouse"/>.
/// Updated atomically alongside each <see cref="StockMovement"/> posting. Exactly one row
/// per (RawMaterialId, WarehouseId) — created on first inbound, never deleted (Quantity goes
/// to zero instead).
/// </summary>
public class StockOnHand : BaseEntity
{
    public int RawMaterialId { get; set; }
    public RawMaterial RawMaterial { get; set; } = null!;

    public int WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    /// <summary>Current quantity in this warehouse, in the raw material's UoM.</summary>
    public decimal Quantity { get; set; }
}
