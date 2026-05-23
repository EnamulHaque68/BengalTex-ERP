using BengalTex.ERP.Domain.Entities;

namespace BengalTex.ERP.Application.Quotations;

/// <summary>Single source of the quotation-line costing formula. Computes and stamps the
/// derived UnitCost / UnitPrice / LineTotal onto a line from its cost components.</summary>
public static class QuotationCosting
{
    public static void Compute(QuotationLine line)
    {
        var baseCost = line.MaterialCost + line.LaborCost + line.MachineCost + line.OverheadCost;
        line.UnitCost = Round(baseCost * (1 + line.WastagePercent / 100m));
        line.UnitPrice = Round(line.UnitCost * (1 + line.MarginPercent / 100m));
        line.LineTotal = Round(line.UnitPrice * line.Quantity);
    }

    private static decimal Round(decimal v) => Math.Round(v, 4, MidpointRounding.AwayFromZero);
}
