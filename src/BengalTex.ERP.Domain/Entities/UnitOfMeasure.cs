using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Unit of measure master (UoM). Supports conversion within a UnitType category via BaseUnit self-reference.
/// Convention: ConversionFactor = "1 of this unit equals X base units."
///   Example: For Count category with PCS as base, DOZEN has BaseUnitId=PCS, ConversionFactor=12.
///   For the base unit itself, BaseUnitId is null and ConversionFactor is 1.
///
/// Named "UnitOfMeasure" instead of "Unit" to avoid conflict with MediatR.Unit.
/// </summary>
public class UnitOfMeasure : BaseEntity
{
    public string Code { get; set; } = string.Empty;       // PCS, KG, MTR, DZN
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;     // pcs, kg, m
    public UnitType UnitType { get; set; }

    public int? BaseUnitId { get; set; }                   // null = this IS the base for its category
    public UnitOfMeasure? BaseUnit { get; set; }
    public decimal ConversionFactor { get; set; } = 1m;    // 1 of this unit = ConversionFactor base units

    public bool IsActive { get; set; } = true;
}

public enum UnitType
{
    Count = 1,      // Pcs, Dozen, Box, Gross
    Weight = 2,     // Kg, Gram, Ton, Pound
    Length = 3,     // Meter, Yard, Foot, Inch
    Volume = 4,     // Liter, Gallon, Milliliter
    Area = 5        // SqMeter, SqFoot
}
