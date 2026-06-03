namespace BengalTex.ERP.Shared.Permissions;

/// <summary>
/// Hard-coded permission constants. Permissions are static (defined in code);
/// only role-permission assignments are stored in DB.
/// Format: "{Resource}.{Action}"
/// </summary>
public static class Permissions
{
    public static class Users
    {
        public const string View = "Users.View";
        public const string Create = "Users.Create";
        public const string Edit = "Users.Edit";
        public const string Delete = "Users.Delete";
        public const string ManageRoles = "Users.ManageRoles";
        public const string ApproveDeviceChange = "Users.ApproveDeviceChange";
        public const string ForceUnbindDevice = "Users.ForceUnbindDevice";
    }

    public static class Roles
    {
        public const string View = "Roles.View";
        public const string Create = "Roles.Create";
        public const string Edit = "Roles.Edit";
        public const string Delete = "Roles.Delete";
        public const string ManagePermissions = "Roles.ManagePermissions";
    }

    public static class Companies
    {
        public const string View = "Companies.View";
        public const string Edit = "Companies.Edit";
    }

    public static class Factories
    {
        public const string View = "Factories.View";
        public const string Create = "Factories.Create";
        public const string Edit = "Factories.Edit";
        public const string Delete = "Factories.Delete";
        public const string ManageGeoFence = "Factories.ManageGeoFence";
    }

    public static class Customers
    {
        public const string View = "Customers.View";
        public const string Create = "Customers.Create";
        public const string Edit = "Customers.Edit";
        public const string Delete = "Customers.Delete";
        public const string ManageCreditLimit = "Customers.ManageCreditLimit";
    }

    public static class Suppliers
    {
        public const string View = "Suppliers.View";
        public const string Create = "Suppliers.Create";
        public const string Edit = "Suppliers.Edit";
        public const string Delete = "Suppliers.Delete";
    }

    public static class ProductCategories
    {
        public const string View = "ProductCategories.View";
        public const string Create = "ProductCategories.Create";
        public const string Edit = "ProductCategories.Edit";
        public const string Delete = "ProductCategories.Delete";
    }

    public static class Products
    {
        public const string View = "Products.View";
        public const string Create = "Products.Create";
        public const string Edit = "Products.Edit";
        public const string Delete = "Products.Delete";
        public const string Approve = "Products.Approve";
    }

    public static class RawMaterials
    {
        public const string View = "RawMaterials.View";
        public const string Create = "RawMaterials.Create";
        public const string Edit = "RawMaterials.Edit";
        public const string Delete = "RawMaterials.Delete";
    }

    public static class Boms
    {
        public const string View = "Boms.View";
        public const string Create = "Boms.Create";
        public const string Edit = "Boms.Edit";
        public const string Delete = "Boms.Delete";
        public const string Approve = "Boms.Approve";
    }

    public static class SalesOrders
    {
        public const string View = "SalesOrders.View";
        public const string Create = "SalesOrders.Create";
        public const string Edit = "SalesOrders.Edit";
        public const string Delete = "SalesOrders.Delete";
        public const string Confirm = "SalesOrders.Confirm";
        public const string Cancel = "SalesOrders.Cancel";
    }

    public static class Quotations
    {
        public const string View = "Quotations.View";
        public const string Create = "Quotations.Create";
        public const string Edit = "Quotations.Edit";
        public const string Delete = "Quotations.Delete";
        public const string Send = "Quotations.Send";         // send / accept / reject / revise
        public const string Convert = "Quotations.Convert";    // convert an accepted quotation to a Sales Order
    }

    public static class Samples
    {
        public const string View = "Samples.View";
        public const string Create = "Samples.Create";
        public const string Edit = "Samples.Edit";
        public const string Delete = "Samples.Delete";
        public const string Manage = "Samples.Manage";        // advance lifecycle: start dev / submit / approve / reject
    }

    public static class PurchaseOrders
    {
        public const string View = "PurchaseOrders.View";
        public const string Create = "PurchaseOrders.Create";
        public const string Edit = "PurchaseOrders.Edit";
        public const string Delete = "PurchaseOrders.Delete";
        public const string Approve = "PurchaseOrders.Approve";
    }

    public static class PurchaseRequisitions
    {
        public const string View = "PurchaseRequisitions.View";
        public const string Create = "PurchaseRequisitions.Create";
        public const string Edit = "PurchaseRequisitions.Edit";
        public const string Delete = "PurchaseRequisitions.Delete";
        public const string Submit = "PurchaseRequisitions.Submit";    // submit / cancel
        public const string Decide = "PurchaseRequisitions.Decide";    // approve / reject
        public const string Convert = "PurchaseRequisitions.Convert";  // create PO from approved PR
    }

    public static class GatePasses
    {
        public const string View = "GatePasses.View";
        public const string Create = "GatePasses.Create";
        public const string Edit = "GatePasses.Edit";
        public const string Delete = "GatePasses.Delete";
        public const string Close = "GatePasses.Close";   // close / mark returned / cancel
    }

    public static class GoodsReceipts
    {
        public const string View = "GoodsReceipts.View";
        public const string Create = "GoodsReceipts.Create";
        public const string Edit = "GoodsReceipts.Edit";
        public const string Delete = "GoodsReceipts.Delete";
        public const string Post = "GoodsReceipts.Post";
    }

    public static class DeliveryNotes
    {
        public const string View = "DeliveryNotes.View";
        public const string Create = "DeliveryNotes.Create";
        public const string Edit = "DeliveryNotes.Edit";
        public const string Delete = "DeliveryNotes.Delete";
        public const string Post = "DeliveryNotes.Post";
    }

    public static class Production
    {
        public const string View = "Production.View";
        public const string Create = "Production.Create";
        public const string Edit = "Production.Edit";
        public const string IssueMaterial = "Production.IssueMaterial";
        public const string ReceiveFinishedGoods = "Production.ReceiveFinishedGoods";
        public const string ManageStages = "Production.ManageStages";   // start / complete / skip routing stages
    }

    public static class Wastage
    {
        public const string View = "Wastage.View";
        public const string Create = "Wastage.Create";
        public const string Edit = "Wastage.Edit";
        public const string Delete = "Wastage.Delete";
        public const string ManageReasons = "Wastage.ManageReasons";
    }

    public static class Machines
    {
        public const string View = "Machines.View";
        public const string Create = "Machines.Create";
        public const string Edit = "Machines.Edit";
        public const string Delete = "Machines.Delete";
    }

    public static class JobCards
    {
        public const string View = "JobCards.View";
        public const string Create = "JobCards.Create";
        public const string Edit = "JobCards.Edit";
        public const string Delete = "JobCards.Delete";
        public const string Scan = "JobCards.Scan";    // operator action — Start/Pause/Resume/Complete
    }

    public static class Leaves
    {
        public const string View = "Leaves.View";              // see own + team leave
        public const string Apply = "Leaves.Apply";            // submit own leave application
        public const string ViewAll = "Leaves.ViewAll";        // HR — see everyone's leave
        public const string Approve = "Leaves.Approve";        // approve / reject pending applications
        public const string Cancel = "Leaves.Cancel";          // cancel approved leave (HR / employee for own)
        public const string ManageTypes = "Leaves.ManageTypes";        // HR — leave type master
        public const string ManageHolidays = "Leaves.ManageHolidays";  // HR — holiday calendar
        public const string ManageBalances = "Leaves.ManageBalances";  // HR — initialize / adjust balances
    }

    public static class EmployeeLoans
    {
        public const string View = "EmployeeLoans.View";
        public const string Create = "EmployeeLoans.Create";
        public const string Edit = "EmployeeLoans.Edit";
        public const string Close = "EmployeeLoans.Close";    // cancel / write-off
    }

    public static class FestivalBonuses
    {
        public const string View = "FestivalBonuses.View";
        public const string Create = "FestivalBonuses.Create";
        public const string Edit = "FestivalBonuses.Edit";
        public const string Delete = "FestivalBonuses.Delete";
        public const string Pay = "FestivalBonuses.Pay";      // mark paid + auto-journal
    }

    public static class Compliance
    {
        public const string View = "Compliance.View";
        public const string ManageCertificates = "Compliance.ManageCertificates";   // create / edit / delete certs
        public const string ScheduleAudit = "Compliance.ScheduleAudit";             // create audits
        public const string RecordAudit = "Compliance.RecordAudit";                 // edit audit result + findings
        public const string ManageCap = "Compliance.ManageCap";                     // edit findings / close CAP items
    }

    public static class MasterSetup
    {
        public const string View = "MasterSetup.View";
        public const string ManageDepartments = "MasterSetup.ManageDepartments";
        public const string ManageDesignations = "MasterSetup.ManageDesignations";
        public const string ManageShifts = "MasterSetup.ManageShifts";
        public const string ManageBankAccounts = "MasterSetup.ManageBankAccounts";
    }

    public static class BankReconciliation
    {
        public const string View = "BankReconciliation.View";
        public const string Manage = "BankReconciliation.Manage";   // create/edit statements + match + reconcile
    }

    public static class Inventory
    {
        public const string View = "Inventory.View";
        public const string Adjust = "Inventory.Adjust";
        public const string Transfer = "Inventory.Transfer";
    }

    public static class Invoices
    {
        public const string View = "Invoices.View";
        public const string Create = "Invoices.Create";
        public const string Edit = "Invoices.Edit";
        public const string Delete = "Invoices.Delete";
    }

    public static class Payments
    {
        public const string View = "Payments.View";
        public const string Create = "Payments.Create";
        public const string Edit = "Payments.Edit";
    }

    public static class Returns
    {
        public const string View = "Returns.View";
        public const string Create = "Returns.Create";
        public const string Edit = "Returns.Edit";
        public const string Delete = "Returns.Delete";
        public const string Post = "Returns.Post";
    }

    public static class ProformaInvoices
    {
        public const string View = "ProformaInvoices.View";
        public const string Create = "ProformaInvoices.Create";
        public const string Edit = "ProformaInvoices.Edit";
        public const string Delete = "ProformaInvoices.Delete";
        public const string Send = "ProformaInvoices.Send";         // send / accept / expire / cancel
        public const string Convert = "ProformaInvoices.Convert";    // create real CustomerInvoice from accepted Proforma
    }

    public static class CreditNotes
    {
        public const string View = "CreditNotes.View";
        public const string Create = "CreditNotes.Create";
        public const string Edit = "CreditNotes.Edit";
        public const string Delete = "CreditNotes.Delete";
        public const string Issue = "CreditNotes.Issue";             // issue / cancel — posts/reverses journal + AR settle
    }

    public static class DebitNotes
    {
        public const string View = "DebitNotes.View";
        public const string Create = "DebitNotes.Create";
        public const string Edit = "DebitNotes.Edit";
        public const string Delete = "DebitNotes.Delete";
        public const string Issue = "DebitNotes.Issue";              // issue / cancel — posts/reverses journal + AP settle
    }

    public static class Qc
    {
        public const string View = "Qc.View";
        public const string Create = "Qc.Create";
        public const string Edit = "Qc.Edit";
        public const string Delete = "Qc.Delete";
        public const string Post = "Qc.Post";
    }

    public static class Employees
    {
        public const string View = "Employees.View";
        public const string Create = "Employees.Create";
        public const string Edit = "Employees.Edit";
        public const string Delete = "Employees.Delete";
    }

    public static class Attendance
    {
        public const string View = "Attendance.View";
        public const string ViewOwn = "Attendance.ViewOwn";
        public const string CheckIn = "Attendance.CheckIn";
        public const string ManualEntry = "Attendance.ManualEntry";
        public const string ApproveFlagged = "Attendance.ApproveFlagged";
        public const string ViewSuspiciousActivity = "Attendance.ViewSuspiciousActivity";
    }

    public static class Payroll
    {
        public const string View = "Payroll.View";        // see payslips
        public const string Process = "Payroll.Process";   // generate / adjust / mark paid / delete
        public const string ExportBankAdvice = "Payroll.ExportBankAdvice";   // download bank-disbursement CSV
        public const string ManageSettlement = "Payroll.ManageSettlement";    // create / approve / cancel final settlements
    }

    public static class Subcontracting
    {
        public const string View = "Subcontracting.View";
        public const string Create = "Subcontracting.Create";
        public const string Edit = "Subcontracting.Edit";
        public const string Delete = "Subcontracting.Delete";
        public const string Issue = "Subcontracting.Issue";      // post material out to subcontractor
        public const string Receive = "Subcontracting.Receive";  // receive processed material back
    }

    public static class Styles
    {
        public const string View = "Styles.View";
        public const string Create = "Styles.Create";
        public const string Edit = "Styles.Edit";
        public const string Delete = "Styles.Delete";
    }

    public static class Notifications
    {
        public const string View = "Notifications.View";
        public const string Send = "Notifications.Send";
    }

    public static class Banking
    {
        public const string View = "Banking.View";
        public const string Create = "Banking.Create";
        public const string Edit = "Banking.Edit";
        public const string Delete = "Banking.Delete";
        public const string Manage = "Banking.Manage";   // open / ship / settle / cancel LC
    }

    public static class Accounting
    {
        public const string View = "Accounting.View";              // see chart of accounts, journals, reports
        public const string ManageAccounts = "Accounting.ManageAccounts";  // create/edit/delete chart-of-accounts nodes
        public const string CreateJournal = "Accounting.CreateJournal";    // create/edit/delete draft journal vouchers
        public const string PostJournal = "Accounting.PostJournal";        // post (freeze) journal vouchers into the ledger
    }

    public static class Expenses
    {
        public const string View = "Expenses.View";
        public const string Create = "Expenses.Create";
        public const string Edit = "Expenses.Edit";
        public const string Delete = "Expenses.Delete";
        public const string Approve = "Expenses.Approve";              // approve + pay (posts to ledger) / cancel
        public const string ManageCategories = "Expenses.ManageCategories";
    }

    public static class Reports
    {
        public const string ViewSales = "Reports.ViewSales";
        public const string ViewPurchase = "Reports.ViewPurchase";
        public const string ViewInventory = "Reports.ViewInventory";
        public const string ViewProduction = "Reports.ViewProduction";   // gates WIP + Production Summary
        public const string ViewHr = "Reports.ViewHr";
        public const string ViewFinance = "Reports.ViewFinance";
        public const string Export = "Reports.Export";
    }

    public static class Dashboard
    {
        public const string ViewOwner = "Dashboard.ViewOwner";
        public const string ViewProduction = "Dashboard.ViewProduction";
        public const string ViewAccounts = "Dashboard.ViewAccounts";
        public const string ViewHr = "Dashboard.ViewHr";
        public const string ViewSales = "Dashboard.ViewSales";
    }

    public static class Settings
    {
        public const string View = "Settings.View";
        public const string Edit = "Settings.Edit";
        public const string ManageNumberingSeries = "Settings.ManageNumberingSeries";
    }

    public static class Currencies
    {
        public const string View = "Currencies.View";
        public const string Create = "Currencies.Create";
        public const string Edit = "Currencies.Edit";
        public const string Delete = "Currencies.Delete";
    }

    public static class UnitsOfMeasure
    {
        public const string View = "UnitsOfMeasure.View";
        public const string Create = "UnitsOfMeasure.Create";
        public const string Edit = "UnitsOfMeasure.Edit";
        public const string Delete = "UnitsOfMeasure.Delete";
    }

    public static class Warehouses
    {
        public const string View = "Warehouses.View";
        public const string Create = "Warehouses.Create";
        public const string Edit = "Warehouses.Edit";
        public const string Delete = "Warehouses.Delete";
    }

    public static class AuditLog
    {
        public const string View = "AuditLog.View";
    }

    public static class Attachments
    {
        public const string View = "Attachments.View";     // list + download
        public const string Manage = "Attachments.Manage"; // upload + delete
    }

    public static class Approvals
    {
        public const string View = "Approvals.View";   // see approval requests / inbox
        public const string Act = "Approvals.Act";      // approve / reject pending requests
    }

    /// <summary>
    /// Returns all permission strings via reflection. Used for seeding and admin UI.
    /// </summary>
    public static IReadOnlyList<string> GetAll()
    {
        var result = new List<string>();
        foreach (var nested in typeof(Permissions).GetNestedTypes())
        {
            foreach (var field in nested.GetFields(System.Reflection.BindingFlags.Public
                                                   | System.Reflection.BindingFlags.Static
                                                   | System.Reflection.BindingFlags.FlattenHierarchy))
            {
                if (field.IsLiteral && !field.IsInitOnly && field.FieldType == typeof(string))
                {
                    var value = field.GetRawConstantValue() as string;
                    if (value is not null) result.Add(value);
                }
            }
        }
        return result;
    }
}