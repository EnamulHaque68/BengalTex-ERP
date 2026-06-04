using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Fixed Asset register — capital items (machinery, vehicles, computers, furniture) that
/// depreciate over time and sit in the asset section of the balance sheet. v1 uses
/// straight-line depreciation only: monthly depreciation = (AcquisitionCost − SalvageValue) /
/// (UsefulLifeYears × 12). Running depreciation auto-posts Dr Depreciation Expense (5320) /
/// Cr Accumulated Depreciation (1215). On disposal, the accumulated depreciation is reversed
/// against the asset's gross cost, and any gain/loss against the disposal proceeds is recorded.
/// </summary>
public class FixedAsset : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;        // "FA-####"

    public string Name { get; set; } = string.Empty;
    public FixedAssetCategory Category { get; set; } = FixedAssetCategory.Machinery;

    /// <summary>Where the asset physically sits (factory floor / office / warehouse).</summary>
    public string? Location { get; set; }

    /// <summary>Linked machine record when this asset IS a tracked production machine.</summary>
    public int? MachineId { get; set; }
    public Machine? Machine { get; set; }

    public DateOnly AcquisitionDate { get; set; }
    public decimal AcquisitionCost { get; set; }
    public decimal SalvageValue { get; set; }            // residual at end of life (BDT)

    /// <summary>Useful life in YEARS. Monthly dep = (Cost − Salvage) / (Years × 12).</summary>
    public int UsefulLifeYears { get; set; }

    public DepreciationMethod DepreciationMethod { get; set; } = DepreciationMethod.StraightLine;

    /// <summary>Running total of depreciation posted (BDT). Updated atomically on each run.</summary>
    public decimal AccumulatedDepreciation { get; set; }

    /// <summary>YYYYMM (e.g. 202607) — last month depreciation was posted for this asset.</summary>
    public int? LastDepreciationYearMonth { get; set; }

    public FixedAssetStatus Status { get; set; } = FixedAssetStatus.Active;

    // ── Disposal fields (populated when Status = Disposed/WrittenOff) ──
    public DateOnly? DisposalDate { get; set; }
    public decimal? DisposalProceeds { get; set; }       // sale value in BDT
    public string? DisposalNotes { get; set; }
    public string? DisposedByUser { get; set; }

    public string? Notes { get; set; }

    /// <summary>NetBookValue = AcquisitionCost − AccumulatedDepreciation. Computed; never stored.</summary>
    public decimal GetNetBookValue() => AcquisitionCost - AccumulatedDepreciation;
}

public enum FixedAssetCategory
{
    Machinery = 1,
    Vehicle = 2,
    OfficeEquipment = 3,
    Furniture = 4,
    Computer = 5,
    Building = 6,
    Other = 99
}

public enum DepreciationMethod
{
    StraightLine = 1
    // WrittenDownValue (WDV) reserved for v1b
}

public enum FixedAssetStatus
{
    Active = 1,
    Disposed = 2,         // sold/scrapped — entity becomes immutable
    WrittenOff = 3        // written off without proceeds
}
