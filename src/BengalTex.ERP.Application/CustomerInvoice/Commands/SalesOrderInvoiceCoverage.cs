using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;

namespace BengalTex.ERP.Application.CustomerInvoice.Commands;

/// <summary>
/// Keeps <see cref="SalesOrderLine.InvoicedQuantity"/> — the single source of truth for full/partial
/// invoice coverage — in sync with the invoice lines that reference it. Invoice lines with a null
/// <c>SalesOrderLineId</c> (ad-hoc / charge lines) are ignored. SO lines are loaded tracked so the
/// caller's SaveChanges persists the change atomically with the invoice.
/// </summary>
internal static class SalesOrderInvoiceCoverage
{
    /// <summary>Releases (subtracts) the invoiced quantity back to the SO lines — on cancel/delete/edit-reverse.</summary>
    public static async Task ReleaseAsync(
        IRepository<SalesOrderLine, long> soLineRepo,
        IEnumerable<(long? SalesOrderLineId, decimal Quantity)> lines,
        CancellationToken ct)
    {
        foreach (var grp in lines.Where(l => l.SalesOrderLineId.HasValue)
                                 .GroupBy(l => l.SalesOrderLineId!.Value))
        {
            var soLine = await soLineRepo.GetByIdAsync(grp.Key, ct);
            if (soLine is null) continue;
            soLine.InvoicedQuantity -= grp.Sum(x => x.Quantity);
            if (soLine.InvoicedQuantity < 0m) soLine.InvoicedQuantity = 0m;   // defensive clamp
            soLineRepo.Update(soLine);
        }
    }
}
