using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// One monthly depreciation run posted as a single batch per (Year, Month). Records the
/// total amount + per-asset breakdown for audit; the actual journal entry is posted alongside
/// (Dr Depreciation Expense 5320 / Cr Accumulated Depreciation 1215). Immutable once posted —
/// to reverse, post a manual journal voucher.
/// </summary>
public class AssetDepreciationRun : BaseTransactionalEntity
{
    public string Code { get; set; } = string.Empty;       // "DEP-####"

    public int Year { get; set; }
    public int Month { get; set; }
    public DateOnly RunDate { get; set; }                  // last day of the month

    public decimal TotalAmount { get; set; }               // sum of all line amounts
    public int AssetCount { get; set; }                    // number of assets included

    public string? PostedByUser { get; set; }

    public string? Notes { get; set; }

    public ICollection<AssetDepreciationRunLine> Lines { get; set; } = new List<AssetDepreciationRunLine>();
}

public class AssetDepreciationRunLine : BaseTransactionalEntity
{
    public long AssetDepreciationRunId { get; set; }
    public AssetDepreciationRun AssetDepreciationRun { get; set; } = null!;

    public long FixedAssetId { get; set; }
    public FixedAsset FixedAsset { get; set; } = null!;

    /// <summary>Snapshot at time of run (BDT).</summary>
    public decimal MonthlyDepreciation { get; set; }

    /// <summary>Accumulated depreciation AFTER this run is applied.</summary>
    public decimal AccumulatedAfter { get; set; }

    /// <summary>Net book value AFTER this run.</summary>
    public decimal NetBookValueAfter { get; set; }
}
