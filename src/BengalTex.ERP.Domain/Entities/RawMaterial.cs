using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Raw material master — yarn, satin, paper board, ink, thread, chemicals, packaging, etc.
/// Stock movements / batch-lot tracking belong to the Inventory module; this entity
/// holds only the catalog definition plus reorder + costing reference data.
/// </summary>
public class RawMaterial : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Specification { get; set; }

    public MaterialCategory Category { get; set; } = MaterialCategory.Other;

    // Stock-keeping unit of measure
    public int UnitOfMeasureId { get; set; }
    public UnitOfMeasure UnitOfMeasure { get; set; } = null!;

    public decimal MinimumStockLevel { get; set; }   // reorder trigger
    public decimal OpeningStock { get; set; }
    public decimal StandardCost { get; set; }        // per-unit cost in base currency

    // Optional preferred supplier — speeds up purchase requisitions
    public int? PreferredSupplierId { get; set; }
    public Supplier? PreferredSupplier { get; set; }

    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public enum MaterialCategory
{
    Yarn = 1,
    Fabric = 2,
    Ink = 3,
    Chemical = 4,
    Thread = 5,
    PaperBoard = 6,
    Packaging = 7,
    Adhesive = 8,
    Other = 99
}
