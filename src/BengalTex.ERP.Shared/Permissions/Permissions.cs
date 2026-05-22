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

    public static class PurchaseOrders
    {
        public const string View = "PurchaseOrders.View";
        public const string Create = "PurchaseOrders.Create";
        public const string Edit = "PurchaseOrders.Edit";
        public const string Delete = "PurchaseOrders.Delete";
        public const string Approve = "PurchaseOrders.Approve";
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
    }

    public static class Reports
    {
        public const string ViewSales = "Reports.ViewSales";
        public const string ViewPurchase = "Reports.ViewPurchase";
        public const string ViewInventory = "Reports.ViewInventory";
        public const string ViewProduction = "Reports.ViewProduction";
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