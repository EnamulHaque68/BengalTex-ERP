using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Domain.Entities;

/// <summary>
/// Phase A6c — a dated foreign-exchange rate for a <see cref="Currency"/>. Unlike
/// <see cref="Currency.ExchangeRateToBase"/> (the single "current" rate used for new transactions),
/// this table keeps the rate history so a rate can be resolved as-of any date — the source for
/// month-end foreign-currency revaluation (Phase A7).
/// </summary>
public class ExchangeRate : BaseEntity
{
    public int CurrencyId { get; set; }
    public Currency Currency { get; set; } = null!;

    public DateOnly RateDate { get; set; }

    /// <summary>BDT per 1 unit of the currency on <see cref="RateDate"/>.</summary>
    public decimal Rate { get; set; }

    /// <summary>Optional source note (e.g. "BB mid-rate", "bank TT").</summary>
    public string? Source { get; set; }
}
