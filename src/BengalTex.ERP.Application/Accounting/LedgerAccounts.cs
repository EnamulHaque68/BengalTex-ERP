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
    public const string VatReceivable = "1170";   // input VAT
    public const string AccountsPayable = "2110";
    public const string VatPayable = "2120";       // output VAT
    public const string SalaryPayable = "2130";
    public const string SalesRevenue = "4100";
    public const string SalesReturnsAllowances = "4150";   // contra-revenue: debited by Credit Notes
    public const string CostOfGoodsSold = "5100";
    public const string PurchaseReturnsAllowances = "5150"; // contra-expense: credited by Debit Notes
    public const string SalaryExpense = "5200";
    public const string AdministrativeExpense = "5400";   // default expense account for unmapped categories
}
