namespace BengalTex.ERP.Application.Payroll;

/// <summary>
/// Bangladesh Labour Act 2006 gratuity rule (industry-common interpretation):
///   • &lt; 5 years service        → 0 (not entitled)
///   • 5 to &lt; 10 years service  → 30 days basic × completed years
///   • ≥ 10 years service        → 60 days basic × completed years
/// Partial year &gt;= 6 months counts as 1 completed year.
/// </summary>
public static class GratuityCalculator
{
    public sealed record Result(decimal Years, decimal Amount);

    public static Result Calculate(DateOnly joiningDate, DateOnly settlementDate, decimal basicSalary)
    {
        if (settlementDate < joiningDate) return new Result(0m, 0m);
        if (basicSalary <= 0m) return new Result(0m, 0m);

        var totalDays = settlementDate.ToDateTime(TimeOnly.MinValue)
                        .Subtract(joiningDate.ToDateTime(TimeOnly.MinValue)).TotalDays;
        var rawYears = totalDays / 365.25;

        var completedYears = (decimal)Math.Floor(rawYears);
        var remainder = rawYears - (double)completedYears;
        if (remainder >= 0.5) completedYears += 1m;

        if (completedYears < 5m) return new Result(completedYears, 0m);

        var dailyRate = basicSalary / 30m;
        var daysPerYear = completedYears >= 10m ? 60m : 30m;
        var amount = Math.Round(dailyRate * daysPerYear * completedYears, 2, MidpointRounding.AwayFromZero);
        return new Result(completedYears, amount);
    }
}
