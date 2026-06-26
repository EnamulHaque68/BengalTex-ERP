namespace BengalTex.ERP.Application.SalesOrder.Queries;

/// <summary>
/// Derives a sales order's production progress from its linked production orders (Phase 1).
/// Pure computation — no stored columns on the sales order (Planning = a view over SO + Production).
///   • Ordered  = Σ line quantities
///   • Produced = Σ quantity of <c>Completed</c> linked production orders
///   • Allocated/Planned = Σ quantity of all non-cancelled linked production orders
/// </summary>
internal static class ProductionProgressCalc
{
    /// <summary>Progress % = produced ÷ ordered, clamped to 0–100, one decimal.</summary>
    public static decimal Percent(decimal ordered, decimal produced)
        => ordered > 0 ? Math.Round(Math.Min(produced / ordered * 100m, 100m), 1) : 0m;

    /// <summary>
    /// Production status label (derived, never stored). Separate axis from the dispatch status:
    ///   NotStarted → no production order yet
    ///   Planning   → production order(s) exist but nothing produced (completed) yet
    ///   PartiallyProduced → some produced, less than ordered
    ///   Produced   → produced ≥ ordered
    /// </summary>
    public static string DeriveStatus(decimal ordered, decimal produced, bool hasAnyProductionOrder)
    {
        if (!hasAnyProductionOrder) return "NotStarted";
        if (produced <= 0m) return "Planning";
        if (ordered > 0m && produced >= ordered) return "Produced";
        return "PartiallyProduced";
    }
}
