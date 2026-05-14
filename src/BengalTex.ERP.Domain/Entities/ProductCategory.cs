using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Product category master. Flat for MVP (no parent/child hierarchy yet).
/// Groups products like "Woven Labels", "Hand Tags", "Stickers", "Packaging".
/// </summary>
public class ProductCategory : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
