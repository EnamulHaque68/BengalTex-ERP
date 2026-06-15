using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// A customer-specific selling price for a product — overrides the product's default sales price
/// when quoting / ordering for that customer. One active price per (Customer, Product). The price
/// is expressed in the customer's usual trading currency; it auto-fills the unit price on Quotation
/// and Sales Order lines (the user can still override per document).
/// </summary>
public class CustomerProductPrice : BaseEntity
{
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    /// <summary>Negotiated unit price for this customer (in their trading currency).</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>The buyer's own code/article-number for this product ("buyer er code = Y"), for export docs.</summary>
    public string? BuyerProductCode { get; set; }

    /// <summary>The buyer's own name/description for this product (optional, shown on export docs).</summary>
    public string? BuyerProductName { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }
}
