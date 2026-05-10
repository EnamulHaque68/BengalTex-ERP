using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Infrastructure.Persistence.CrossCutting;

/// <summary>
/// Configurable document number generator. Format example: BTX/SO/2025/00001
/// </summary>
public class NumberingSeries : BaseEntity
{
    public string Code { get; set; } = string.Empty;       // e.g., "SO", "PO", "INV"
    public string Description { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;     // e.g., "BTX/SO"
    public string Separator { get; set; } = "/";
    public bool IncludeYear { get; set; } = true;
    public bool IncludeMonth { get; set; }
    public int PaddingLength { get; set; } = 5;            // 00001
    public ResetCycle ResetCycle { get; set; } = ResetCycle.Yearly;
    public long CurrentNumber { get; set; }
    public int CurrentYear { get; set; }
    public int CurrentMonth { get; set; }
    public int? FactoryId { get; set; }                    // Optional per-factory series
}

public enum ResetCycle { Never, Yearly, Monthly }