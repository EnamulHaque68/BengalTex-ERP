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
    public const string OpeningBalanceEquity = "3150";      // contra for opening-balance seeding (stock, etc.)
    public const string ScrapSalesIncome = "4250";          // credited when reusable scrap/wastage is sold
}
