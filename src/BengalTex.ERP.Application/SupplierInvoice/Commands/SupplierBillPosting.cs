using BengalTex.ERP.Application.Accounting;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Entities;

namespace BengalTex.ERP.Application.SupplierInvoice.Commands;

/// <summary>
/// Phase A2 — computes the ledger legs for approving a supplier bill, shared by the approve
/// (post as-is) and cancel (post mirrored) handlers so the two are guaranteed symmetric.
///
/// New path (the PO has GL-posted GRNs — goods already hit GR/IR at receipt):
///   Dr GR/IR Clearing 2150   Σ(material qty × PO price × rate)   ← clears the receipt liability
///   Dr/Cr Purchase Price Variance 5155   Σ(qty × (bill price − PO price) × rate)
///   Dr [service line's expense account]  Σ(service qty × price × rate)
///   Dr VAT Receivable 1170
///   Cr Accounts Payable 2110  (gross)
///
/// Legacy path (pre-A2 GRNs, GR/IR init not run — no receipt liability to clear):
///   Dr RM Inventory 1140  (material subtotal) + service + VAT / Cr AP — today's behaviour.
/// </summary>
internal static class SupplierBillPosting
{
    public static List<JournalPostingLine> BuildApprovalLegs(
        Domain.Entities.SupplierInvoice inv,
        Domain.Entities.PurchaseOrder po,
        bool useGrIrPath)
    {
        var rate = inv.ExchangeRate;
        var legs = new List<JournalPostingLine>();

        // PO unit price per raw material (first matching PO line — same basis GRN receipt used).
        var poPriceByRm = po.Lines
            .GroupBy(l => l.RawMaterialId)
            .ToDictionary(g => g.Key, g => g.First().UnitPrice);

        decimal grIrTotal = 0m, inventoryTotal = 0m, ppv = 0m;

        foreach (var line in inv.Lines)
        {
            var lineValue = line.Quantity * line.UnitPrice * rate;

            if (line.AccountId.HasValue && line.Account is not null)
            {
                // Service line → its own expense account (never touches inventory / GR/IR).
                legs.Add(new JournalPostingLine(line.Account.Code, lineValue, 0m));
                continue;
            }

            // Material line.
            if (useGrIrPath)
            {
                var poPrice = line.RawMaterialId.HasValue && poPriceByRm.TryGetValue(line.RawMaterialId.Value, out var p)
                    ? p : line.UnitPrice;   // fall back to bill price → PPV 0 if the RM isn't on the PO
                var poValue = line.Quantity * poPrice * rate;
                grIrTotal += poValue;
                ppv += lineValue - poValue;
            }
            else
            {
                inventoryTotal += lineValue;
            }
        }

        if (useGrIrPath)
        {
            if (grIrTotal != 0m)
                legs.Add(new JournalPostingLine(LedgerAccounts.GrIrClearing, grIrTotal, 0m));
            if (ppv > 0m)
                legs.Add(new JournalPostingLine(LedgerAccounts.PurchasePriceVariance, ppv, 0m));
            else if (ppv < 0m)
                legs.Add(new JournalPostingLine(LedgerAccounts.PurchasePriceVariance, 0m, -ppv));
        }
        else if (inventoryTotal != 0m)
        {
            legs.Add(new JournalPostingLine(LedgerAccounts.RawMaterialInventory, inventoryTotal, 0m));
        }

        if (inv.VatAmount > 0m)
            legs.Add(new JournalPostingLine(LedgerAccounts.VatReceivable, inv.VatAmount * rate, 0m));

        legs.Add(new JournalPostingLine(LedgerAccounts.AccountsPayable, 0m, inv.TotalAmount * rate));

        return legs;
    }

    /// <summary>Mirror of the approval legs (Dr↔Cr swapped) — used on cancel.</summary>
    public static List<JournalPostingLine> Mirror(IEnumerable<JournalPostingLine> legs) =>
        legs.Select(l => new JournalPostingLine(l.AccountCode, l.Credit, l.Debit)).ToList();
}
