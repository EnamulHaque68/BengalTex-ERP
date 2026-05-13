using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// System-wide currency master. Base currency is BDT (IsBaseCurrency = true).
/// Exactly one currency must be flagged as base — enforced by unique filtered index.
/// ExchangeRateToBase is the "current" rate for new transactions; historical rates
/// are captured per transaction via the Money value object.
/// </summary>
public class Currency : BaseEntity
{
    public string Code { get; set; } = string.Empty;        // ISO 4217: BDT, USD, EUR
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;      // ৳, $, €
    public decimal ExchangeRateToBase { get; set; } = 1m;   // 1 unit of this currency = X base units
    public bool IsBaseCurrency { get; set; }
    public bool IsActive { get; set; } = true;
}
