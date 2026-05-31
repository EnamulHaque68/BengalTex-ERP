namespace BengalTex.ERP.Application.Payroll;

/// <summary>
/// Bangladesh personal income tax — slab table (default: male, &lt;65y, non-disabled).
/// FY 2024-25 slabs:
///   0  – 350,000   : 0%
///   next 100,000   : 5%
///   next 300,000   : 10%
///   next 400,000   : 15%
///   next 500,000   : 20%
///   above          : 25%
/// Applied to annualised gross (monthly gross × 12); monthly tax = annualTax / 12.
/// v1a uses one slab table only — exemptions / minimum-tax / female-higher-threshold deferred.
/// </summary>
public static class BangladeshIncomeTax
{
    private static readonly (decimal Width, decimal Rate)[] Slabs =
    {
        (350_000m, 0.00m),
        (100_000m, 0.05m),
        (300_000m, 0.10m),
        (400_000m, 0.15m),
        (500_000m, 0.20m),
        (decimal.MaxValue, 0.25m)
    };

    /// <summary>Compute monthly income tax (BDT) from monthly gross pay.</summary>
    public static decimal MonthlyTaxOf(decimal monthlyGross)
    {
        if (monthlyGross <= 0) return 0;
        var annual = monthlyGross * 12m;
        var remaining = annual;
        decimal tax = 0;
        foreach (var (width, rate) in Slabs)
        {
            if (remaining <= 0) break;
            var slice = Math.Min(remaining, width);
            tax += slice * rate;
            remaining -= slice;
        }
        return Math.Round(tax / 12m, 2, MidpointRounding.AwayFromZero);
    }
}
