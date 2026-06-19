using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// A catalog variant of a <see cref="Product"/> — e.g. a button in Red/Medium, a zipper in
/// 20cm/Black. Replaces the product's plain-text Size/Color fields for products that come in
/// many combinations, and carries an optional buyer/barcode SKU and a per-variant sales price.
///
/// v1 is CATALOG-ONLY: variants describe the product's SKU breakdown but are NOT yet a stock-keeping
/// unit — stock, BOM and transaction lines still track at the Product level. Variant-level stock
/// (variant on StockMovement / order lines) is the invasive v2 enhancement.
/// </summary>
public class ProductVariant : BaseEntity
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    /// <summary>Short variant code, unique within the product (e.g. "RED-M").</summary>
    public string VariantCode { get; set; } = string.Empty;

    /// <summary>Optional descriptive label (e.g. "Red / Medium").</summary>
    public string? Name { get; set; }

    public string? Color { get; set; }
    public string? Size { get; set; }

    /// <summary>Buyer / barcode SKU for this variant.</summary>
    public string? Sku { get; set; }

    /// <summary>When set, overrides the parent product's sales price for this variant.</summary>
    public decimal? SalesPriceOverride { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;
}
