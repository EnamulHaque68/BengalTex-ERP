using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Infrastructure.Services;

/// <summary>Implements <see cref="IExchangeRateResolver"/> over the dated ExchangeRates table.</summary>
public sealed class ExchangeRateResolver : IExchangeRateResolver
{
    private readonly ApplicationDbContext _db;
    public ExchangeRateResolver(ApplicationDbContext db) => _db = db;

    public async Task<decimal> GetRateAsOfAsync(int currencyId, DateOnly date, CancellationToken ct = default)
    {
        var dated = await _db.ExchangeRates.AsNoTracking()
            .Where(r => r.CurrencyId == currencyId && r.RateDate <= date)
            .OrderByDescending(r => r.RateDate)
            .Select(r => (decimal?)r.Rate)
            .FirstOrDefaultAsync(ct);
        if (dated is decimal r0 && r0 > 0m) return r0;

        // Fall back to the currency's current rate (base currency → 1).
        var cur = await _db.Currencies.AsNoTracking()
            .Where(c => c.Id == currencyId)
            .Select(c => new { c.ExchangeRateToBase, c.IsBaseCurrency })
            .FirstOrDefaultAsync(ct);
        if (cur is null) return 1m;
        return cur.IsBaseCurrency ? 1m : (cur.ExchangeRateToBase > 0m ? cur.ExchangeRateToBase : 1m);
    }
}
