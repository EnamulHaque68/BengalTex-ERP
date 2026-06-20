namespace BengalTex.ERP.Application.RawMaterialSubstitutes;

public sealed record RawMaterialSubstituteDto(
    int Id,
    int RawMaterialId,
    int SubstituteRawMaterialId,
    string SubstituteCode,
    string SubstituteName,
    string SubstituteUnit,
    decimal ConversionFactor,
    decimal SubstituteOnHand,        // total stock on hand of the substitute (across warehouses)
    string? Notes,
    bool IsActive);
