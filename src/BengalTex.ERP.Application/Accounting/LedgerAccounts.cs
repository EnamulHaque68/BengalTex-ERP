namespace BengalTex.ERP.Application.Accounting;

/// <summary>
/// Codes of the seeded system Chart-of-Accounts nodes that the auto-journal flows post to.
/// These MUST match <c>DataSeeder.SeedChartOfAccountsAsync</c>.
/// </summary>
public static class LedgerAccounts
{
    public const string Cash = "1110";
    public const string Bank = "1120";
    public const string AccountsReceivable = "1130";
    public const string RawMaterialInventory = "1140";
    public const string FinishedGoodsInventory = "1150";
    public const string WorkInProgressInventory = "1160";   // RM cost parked here from issue until FG receipt (backflush)
    public const string VatReceivable = "1170";   // input VAT
    public const string MachineryEquipment = "1210";       // gross fixed-asset cost (existed)
    public const string AccumulatedDepreciation = "1215";  // contra-asset; credited each month
    public const string AccountsPayable = "2110";
    public const string VatPayable = "2120";       // output VAT
    public const string SalaryPayable = "2130";
    public const string SalesRevenue = "4100";
    public const string SalesReturnsAllowances = "4150";   // contra-revenue: debited by Credit Notes
    public const string CostOfGoodsSold = "5100";
    public const string PurchaseReturnsAllowances = "5150"; // contra-expense: credited by Debit Notes
    public const string SalaryExpense = "5200";
    public const string DepreciationExpense = "5320";      // posted by AssetDepreciation monthly run
    public const string AdministrativeExpense = "5400";   // default expense account for unmapped categories
    public const string GainOnAssetDisposal = "4400";      // credited when disposal proceeds > NBV
    public const string LossOnAssetDisposal = "5350";      // debited when disposal proceeds < NBV
    public const string MaterialWastage = "5700";           // debited by Wastage posts + quarantine Scrap write-offs
    public const string InventoryAdjustment = "5750";       // debited on stock shortage, credited on surplus (count adjustments)
    public const string ExchangeGain = "4300";              // credited on realized FX gain (receipt/payment rate vs invoice rate)
    public const string ExchangeLoss = "5800";              // debited on realized FX loss
    public const string OpeningBalanceEquity = "3150";      // contra for opening-balance seeding (stock, ledger openings)
    public const string ScrapSalesIncome = "4250";          // credited when reusable scrap/wastage is sold

    // ── Phase A1+ (Enterprise Accounting Blueprint) — seeded now, wired per phase ──
    public const string RetainedEarnings = "3200";          // year-end close sweeps net P&L here (existed in seed)
    public const string SupplierAdvance = "1180";           // advance to suppliers (existed in seed)
    public const string CustomerAdvance = "2140";           // advance from customers (existed in seed)
    public const string BankLoan = "2210";                  // long-term bank borrowing (existed in seed)
    public const string BankCharges = "5600";               // bank charges & commission (existed in seed)
    public const string RmInTransit = "1145";               // LC goods shipped, not yet received (Phase A6)
    public const string LcMargin = "1185";                  // LC margin deposits with banks (Phase A6)
    public const string ExportIncentiveReceivable = "1186"; // govt. cash-incentive claims filed (Phase A6)
    public const string EmployeeLoans = "1190";             // employee loan receivable sub-ledger (Phase A5)
    public const string CapitalWorkInProgress = "1220";     // CWIP — assets under construction (Phase A8)
    public const string GrIrClearing = "2150";              // goods-received-not-invoiced clearing (Phase A2)
    public const string AccruedChargesPayable = "2115";     // unsettled landed-cost / C&F agent charges (Phase A2)
    public const string WipAccrual = "2155";                // month-end WIP snapshot contra (Phase A4)
    public const string AitPayable = "2160";                // income tax withheld at source (Phase A5)
    public const string VdsPayable = "2170";                // VAT deducted at source (Phase A5)
    public const string PadLiability = "2180";              // payment-against-documents liability (Phase A6)
    public const string AcceptanceLiability = "2190";       // usance/UPAS acceptance liability (Phase A6)
    public const string ExportIncentiveIncome = "4260";     // cash incentive income (Phase A6)
    public const string UnrealizedExchangeGain = "4310";    // month-end FC revaluation gain (Phase A6)
    public const string UnrealizedExchangeLoss = "5810";    // month-end FC revaluation loss (Phase A6)
    public const string PurchasePriceVariance = "5155";     // PO price vs bill price (Phase A2)
    public const string OverheadAbsorptionVariance = "5165";// under/over-absorbed conversion cost (Phase A4)
    public const string FactoryOverhead = "5300";           // actual factory-overhead pool (Phase A4 true-up)
    public const string AppliedLabour = "5211";             // contra credited when labour is absorbed into WIP (Phase A4)
    public const string AppliedFactoryOverhead = "5311";    // contra credited when FOH is absorbed into WIP (Phase A4)
    public const string SampleMarketingExpense = "5460";    // free samples / marketing (Phase A8)
    public const string InterestExpense = "5860";           // loan / PAD / UPAS interest (Phase A6)
}
