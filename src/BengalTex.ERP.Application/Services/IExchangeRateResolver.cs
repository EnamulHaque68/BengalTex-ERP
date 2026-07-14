namespace BengalTex.ERP.Application.Services;

/// <summary>
/// Phase A6c — resolves a currency's BDT rate as of a date from the dated
/// <c>ExchangeRates</c> history, falling back to the currency's current rate when none is on file.
/// </summary>
public interface IExchangeRateResolver
{
    Task<decimal> GetRateAsOfAsync(int currencyId, DateOnly date, CancellationToken ct = default);
}
