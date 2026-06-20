using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// An approved substitute (alternative) raw material for a primary raw material — used when the
/// primary is short or unavailable. <see cref="ConversionFactor"/> = how many units of the
/// substitute replace one unit of the primary (1 = a straight 1:1 swap). Material-level (applies
/// across every BOM that uses the primary); per-BOM-line precision is a later enhancement.
/// </summary>
public class RawMaterialSubstitute : BaseEntity
{
    public int RawMaterialId { get; set; }
    public RawMaterial RawMaterial { get; set; } = null!;

    public int SubstituteRawMaterialId { get; set; }
    public RawMaterial SubstituteRawMaterial { get; set; } = null!;

    /// <summary>Units of the substitute needed to replace one unit of the primary.</summary>
    public decimal ConversionFactor { get; set; } = 1m;

    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;
}
