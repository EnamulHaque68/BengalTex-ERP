using System.Security.Claims;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Infrastructure.Identity;
using BengalTex.ERP.Infrastructure.Persistence.CrossCutting;
using BengalTex.ERP.Shared.Permissions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Infrastructure.Persistence;

public class DataSeeder : IDataSeeder
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;

    public DataSeeder(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _db = db;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        // Order matters — Factory depends on Company, Warehouse depends on Factory,
        // SuperAdmin needs Factory for FactoryId assignment.
        await SeedRolesAsync();
        await SeedPermissionsAsync();
        await SeedCompanyAsync(ct);
        await SeedFactoryAsync(ct);
        await SeedCurrenciesAsync(ct);
        await SeedUnitsOfMeasureAsync(ct);
        await SeedWarehousesAsync(ct);
        await SeedProductCategoriesAsync(ct);
        await SeedSuperAdminAsync(ct);
        await SeedNumberingSeriesAsync(ct);
        await SeedChartOfAccountsAsync(ct);
        await SeedExpenseCategoriesAsync(ct);
        await SeedWastageReasonsAsync(ct);
        await SeedLeaveTypesAsync(ct);
    }

    // ─── Leave Types ───────────────────────────────────────────────────────────

    /// <summary>Seeds standard Bangladesh-factory leave types. Idempotent by Code.</summary>
    private async Task SeedLeaveTypesAsync(CancellationToken ct)
    {
        var defs = new (string Code, string Name, bool IsPaid, decimal Entitlement, int? MaxConsec)[]
        {
            ("CL", "Casual Leave",   true,  10m, 3),
            ("SL", "Sick Leave",     true,  14m, null),
            ("AL", "Annual Leave",   true,  10m, null),
            ("ML", "Maternity Leave", true, 112m, null),  // BD Labour Law standard
            ("UL", "Unpaid Leave",   false, 0m,  null),
        };
        var existing = (await _db.LeaveTypes.IgnoreQueryFilters().Select(t => t.Code).ToListAsync(ct)).ToHashSet();
        foreach (var d in defs)
        {
            if (existing.Contains(d.Code)) continue;
            _db.LeaveTypes.Add(new LeaveType
            {
                Code = d.Code, Name = d.Name, IsPaid = d.IsPaid,
                AnnualEntitlement = d.Entitlement, MaxConsecutiveDays = d.MaxConsec, IsActive = true
            });
        }
        await _db.SaveChangesAsync(ct);
    }

    // ─── Wastage Reasons ───────────────────────────────────────────────────────

    private async Task SeedWastageReasonsAsync(CancellationToken ct)
    {
        var defs = new (string Name, bool Reusable)[]
        {
            ("Setup / Startup Waste", false),
            ("Machine Fault", false),
            ("Quality Reject", false),
            ("Trim / Cutting Waste", true),
            ("Color / Shade Mismatch", false),
            ("Operator Error", false),
            ("Material Defect", false),
            ("Excess Production", true),
        };
        var existing = (await _db.WastageReasons.IgnoreQueryFilters().Select(r => r.Name).ToListAsync(ct)).ToHashSet();
        foreach (var d in defs)
        {
            if (existing.Contains(d.Name)) continue;
            _db.WastageReasons.Add(new WastageReason { Name = d.Name, IsReusable = d.Reusable, IsActive = true });
        }
        await _db.SaveChangesAsync(ct);
    }

    // ─── Expense Categories ────────────────────────────────────────────────────

    /// <summary>Seeds common expense categories mapped to their expense ledger account (by code).
    /// Idempotent by Name. Requires the Chart of Accounts to be seeded first.</summary>
    private async Task SeedExpenseCategoriesAsync(CancellationToken ct)
    {
        var defs = new (string Name, string AccountCode)[]
        {
            ("Office Rent", "5400"),
            ("Electricity", "5300"),
            ("Internet & Telephone", "5400"),
            ("Transport & Fuel", "5500"),
            ("Machine Maintenance", "5300"),
            ("Printing & Stationery", "5400"),
            ("Packaging Expense", "5300"),
            ("Entertainment", "5400"),
            ("Bank Charges", "5600"),
            ("Miscellaneous", "5400"),
        };

        var existing = (await _db.ExpenseCategories.IgnoreQueryFilters()
            .Select(c => c.Name).ToListAsync(ct)).ToHashSet();
        var accounts = await _db.Accounts.IgnoreQueryFilters()
            .ToDictionaryAsync(a => a.Code, a => a.Id, ct);

        foreach (var d in defs)
        {
            if (existing.Contains(d.Name)) continue;
            _db.ExpenseCategories.Add(new ExpenseCategory
            {
                Name = d.Name,
                LedgerAccountId = accounts.TryGetValue(d.AccountCode, out var id) ? id : null,
                IsActive = true
            });
        }
        await _db.SaveChangesAsync(ct);
    }

    // ─── Chart of Accounts ─────────────────────────────────────────────────────

    /// <summary>
    /// Seeds a standard garments-accessories Chart of Accounts: 5 top-level group accounts
    /// (Assets/Liabilities/Equity/Income/Expense) with the detail accounts the auto-journal
    /// flows will post to. Idempotent — only adds accounts whose Code doesn't already exist.
    /// </summary>
    private async Task SeedChartOfAccountsAsync(CancellationToken ct)
    {
        // (Code, Name, Type, IsGroup, ParentCode) — parents must precede children.
        var defs = new (string Code, string Name, AccountType Type, bool IsGroup, string? Parent)[]
        {
            // ── Assets (1000) ──
            ("1000", "Assets", AccountType.Asset, true, null),
            ("1100", "Current Assets", AccountType.Asset, true, "1000"),
            ("1110", "Cash in Hand", AccountType.Asset, false, "1100"),
            ("1120", "Bank Accounts", AccountType.Asset, false, "1100"),
            ("1130", "Accounts Receivable", AccountType.Asset, false, "1100"),
            ("1140", "Raw Material Inventory", AccountType.Asset, false, "1100"),
            ("1150", "Finished Goods Inventory", AccountType.Asset, false, "1100"),
            ("1160", "Work In Progress (WIP)", AccountType.Asset, false, "1100"),
            ("1170", "VAT Receivable (Input VAT)", AccountType.Asset, false, "1100"),
            ("1180", "Advance to Suppliers", AccountType.Asset, false, "1100"),
            ("1200", "Fixed Assets", AccountType.Asset, true, "1000"),
            ("1210", "Machinery & Equipment", AccountType.Asset, false, "1200"),

            // ── Liabilities (2000) ──
            ("2000", "Liabilities", AccountType.Liability, true, null),
            ("2100", "Current Liabilities", AccountType.Liability, true, "2000"),
            ("2110", "Accounts Payable", AccountType.Liability, false, "2100"),
            ("2120", "VAT Payable (Output VAT)", AccountType.Liability, false, "2100"),
            ("2130", "Salary Payable", AccountType.Liability, false, "2100"),
            ("2140", "Advance from Customers", AccountType.Liability, false, "2100"),
            ("2200", "Long Term Liabilities", AccountType.Liability, true, "2000"),
            ("2210", "Bank Loan", AccountType.Liability, false, "2200"),

            // ── Equity (3000) ──
            ("3000", "Equity", AccountType.Equity, true, null),
            ("3100", "Owner's Capital", AccountType.Equity, false, "3000"),
            ("3200", "Retained Earnings", AccountType.Equity, false, "3000"),
            ("3300", "Owner's Drawings", AccountType.Equity, false, "3000"),

            // ── Income (4000) ──
            ("4000", "Income", AccountType.Income, true, null),
            ("4100", "Sales Revenue", AccountType.Income, false, "4000"),
            ("4150", "Sales Returns & Allowances", AccountType.Income, false, "4000"),
            ("4200", "Other Income", AccountType.Income, false, "4000"),
            ("4300", "Exchange Gain", AccountType.Income, false, "4000"),

            // ── Expenses (5000) ──
            ("5000", "Expenses", AccountType.Expense, true, null),
            ("5100", "Cost of Goods Sold", AccountType.Expense, false, "5000"),
            ("5150", "Purchase Returns & Allowances", AccountType.Expense, false, "5000"),
            ("5200", "Salary & Wages", AccountType.Expense, false, "5000"),
            ("5300", "Factory Overhead", AccountType.Expense, false, "5000"),
            ("5400", "Administrative Expense", AccountType.Expense, false, "5000"),
            ("5500", "Selling & Distribution Expense", AccountType.Expense, false, "5000"),
            ("5600", "Bank Charges", AccountType.Expense, false, "5000"),
            ("5700", "Material Wastage", AccountType.Expense, false, "5000"),
            ("5800", "Exchange Loss", AccountType.Expense, false, "5000"),
        };

        var existingCodes = (await _db.Accounts.IgnoreQueryFilters()
            .Select(a => a.Code).ToListAsync(ct)).ToHashSet();
        var byCode = new Dictionary<string, Account>();

        foreach (var d in defs)
        {
            if (existingCodes.Contains(d.Code)) continue;

            int? parentId = null;
            if (d.Parent is not null && byCode.TryGetValue(d.Parent, out var p))
                parentId = p.Id;   // resolved after each batch save below; see two-pass note

            var account = new Account
            {
                Code = d.Code,
                Name = d.Name,
                AccountType = d.Type,
                IsGroup = d.IsGroup,
                ParentAccountId = parentId,
                IsSystem = true,
                IsActive = true
            };
            byCode[d.Code] = account;
            _db.Accounts.Add(account);
            // Save immediately so the generated Id is available as a parent for the next rows.
            await _db.SaveChangesAsync(ct);
        }
    }

    // ─── Roles ───────────────────────────────────────────────────────────────

    private async Task SeedRolesAsync()
    {
        var roles = new[]
        {
            new ApplicationRole { Name = "SuperAdmin",        Description = "Full system access — all permissions",          IsSystemRole = true },
            new ApplicationRole { Name = "Admin",             Description = "Administrative access",                          IsSystemRole = true },
            new ApplicationRole { Name = "AccountsManager",   Description = "Accounts, finance and invoice management",       IsSystemRole = false },
            new ApplicationRole { Name = "HRManager",         Description = "HR, attendance and payroll management",          IsSystemRole = false },
            new ApplicationRole { Name = "ProductionManager", Description = "Production, inventory and BOM management",       IsSystemRole = false },
            new ApplicationRole { Name = "SalesManager",      Description = "Sales, customers and order management",          IsSystemRole = false },
            new ApplicationRole { Name = "Viewer",            Description = "Read-only access to all modules",               IsSystemRole = false },
        };

        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role.Name!))
                await _roleManager.CreateAsync(role);
        }
    }

    // ─── Permissions per role ─────────────────────────────────────────────────

    private async Task SeedPermissionsAsync()
    {
        var all = Permissions.GetAll();

        await AssignPermissionsAsync("SuperAdmin", all);

        await AssignPermissionsAsync("Admin", all.Where(p =>
            !p.Contains("ForceUnbind") &&
            !p.Contains("ApproveDeviceChange") &&
            !p.StartsWith("Settings.")));

        await AssignPermissionsAsync("AccountsManager", all.Where(p =>
            p.StartsWith("Invoices.") ||
            p.StartsWith("Payments.") ||
            p == Permissions.Reports.ViewFinance ||
            p == Permissions.Reports.Export ||
            p == Permissions.Dashboard.ViewAccounts ||
            p == Permissions.AuditLog.View ||
            p.StartsWith("Attachments.") ||
            p.StartsWith("Banking.") ||
            p.StartsWith("Accounting.") ||
            p.StartsWith("Expenses.") ||
            p.StartsWith("BankReconciliation.") ||
            p == Permissions.MasterSetup.View ||
            p == Permissions.MasterSetup.ManageBankAccounts ||
            p == Permissions.Approvals.View ||
            p == Permissions.Notifications.View ||
            p == Permissions.Customers.View ||
            p == Permissions.Suppliers.View));

        await AssignPermissionsAsync("HRManager", all.Where(p =>
            p.StartsWith("Employees.") ||
            p.StartsWith("Attendance.") ||
            p.StartsWith("Payroll.") ||
            p.StartsWith("Leaves.") ||
            p.StartsWith("EmployeeLoans.") ||
            p.StartsWith("FestivalBonuses.") ||
            p.StartsWith("Compliance.") ||
            p.StartsWith("Attachments.") ||
            p == Permissions.MasterSetup.View ||
            p == Permissions.MasterSetup.ManageDepartments ||
            p == Permissions.MasterSetup.ManageDesignations ||
            p == Permissions.MasterSetup.ManageShifts ||
            p == Permissions.Reports.ViewHr ||
            p == Permissions.Reports.Export ||
            p == Permissions.Dashboard.ViewHr));

        await AssignPermissionsAsync("ProductionManager", all.Where(p =>
            p.StartsWith("Production.") ||
            p.StartsWith("Inventory.") ||
            p.StartsWith("Boms.") ||
            p.StartsWith("RawMaterials.") ||
            p.StartsWith("Returns.") ||
            p.StartsWith("Qc.") ||
            p.StartsWith("Attachments.") ||
            p.StartsWith("Subcontracting.") ||
            p.StartsWith("Wastage.") ||
            p.StartsWith("Machines.") ||
            p.StartsWith("JobCards.") ||
            p == Permissions.Approvals.View ||
            p == Permissions.Notifications.View ||
            p == Permissions.Reports.ViewProduction ||
            p == Permissions.Reports.ViewInventory ||
            p == Permissions.Reports.Export ||
            p == Permissions.Dashboard.ViewProduction ||
            p == Permissions.Products.View));

        await AssignPermissionsAsync("SalesManager", all.Where(p =>
            p.StartsWith("SalesOrders.") ||
            p.StartsWith("Quotations.") ||
            p.StartsWith("Samples.") ||
            p.StartsWith("Customers.") ||
            p.StartsWith("Returns.") ||
            p.StartsWith("Attachments.") ||
            p.StartsWith("Styles.") ||
            p == Permissions.Invoices.View ||
            p == Permissions.Payments.View ||
            p == Permissions.Reports.ViewSales ||
            p == Permissions.Reports.Export ||
            p == Permissions.Dashboard.ViewSales ||
            p == Permissions.Products.View));

        await AssignPermissionsAsync("Viewer", all.Where(p =>
            p.EndsWith(".View") || p.EndsWith(".ViewOwn")));
    }

    private async Task AssignPermissionsAsync(string roleName, IEnumerable<string> permissions)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role is null) return;

        var existing = (await _roleManager.GetClaimsAsync(role))
            .Where(c => c.Type == "permission")
            .Select(c => c.Value)
            .ToHashSet();

        foreach (var permission in permissions)
        {
            if (!existing.Contains(permission))
                await _roleManager.AddClaimAsync(role, new Claim("permission", permission));
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Marks an entity's soft-delete fields as cleared. Used to revive seeded rows
    /// that were soft-deleted in admin UIs — keeps SQL unique constraints from
    /// blocking re-seed since the row was never physically removed.
    /// </summary>
    private static void Restore<T>(T entity) where T : Domain.Common.ISoftDeletable
    {
        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.DeletedBy = null;
    }

    // ─── Company ──────────────────────────────────────────────────────────────

    private async Task SeedCompanyAsync(CancellationToken ct)
    {
        // IgnoreQueryFilters so we also see soft-deleted rows — SQL unique constraints
        // ignore IsDeleted, so we must compare against ALL physical rows.
        var existing = await _db.Companies
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
        {
            if (existing.IsDeleted)
            {
                Restore(existing);
                await _db.SaveChangesAsync(ct);
            }
            return;
        }

        _db.Companies.Add(new Company
        {
            Name = "Bengal TEX Accessories Ltd.",
            ShortName = "Bengal TEX",
            AddressLine1 = "Dhaka, Bangladesh",
            City = "Dhaka",
            District = "Dhaka",
            Country = "Bangladesh",
            IsActive = true
        });

        await _db.SaveChangesAsync(ct);
    }

    // ─── Factory ──────────────────────────────────────────────────────────────

    private async Task SeedFactoryAsync(CancellationToken ct)
    {
        var existing = await _db.Factories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.Code == "HQ", ct);

        if (existing is not null)
        {
            if (existing.IsDeleted)
            {
                Restore(existing);
                await _db.SaveChangesAsync(ct);
            }
            return;
        }

        var company = await _db.Companies
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(ct);
        if (company is null) return;

        _db.Factories.Add(new Factory
        {
            Code = "HQ",
            Name = "Head Office Factory",
            CompanyId = company.Id,
            AddressLine1 = "Dhaka, Bangladesh",
            City = "Dhaka",
            District = "Dhaka",
            // Dhaka downtown coordinates — 100m geo-fence radius for attendance check-in
            GeoFenceLat = 23.8103,
            GeoFenceLng = 90.4125,
            GeoFenceRadiusMeters = 100,
            IsActive = true
        });

        await _db.SaveChangesAsync(ct);
    }

    // ─── Currencies ───────────────────────────────────────────────────────────

    private async Task SeedCurrenciesAsync(CancellationToken ct)
    {
        var currencies = new[]
        {
            new Currency { Code = "BDT", Name = "Bangladeshi Taka", Symbol = "৳", ExchangeRateToBase = 1m,   IsBaseCurrency = true,  IsActive = true },
            new Currency { Code = "USD", Name = "US Dollar",        Symbol = "$", ExchangeRateToBase = 110m, IsBaseCurrency = false, IsActive = true },
        };

        foreach (var c in currencies)
        {
            var existing = await _db.Currencies
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Code == c.Code, ct);

            if (existing is null)
                _db.Currencies.Add(c);
            else if (existing.IsDeleted)
                Restore(existing);
        }
        await _db.SaveChangesAsync(ct);
    }

    // ─── Units of Measure (2-pass insert for self-FK) ─────────────────────────

    private async Task SeedUnitsOfMeasureAsync(CancellationToken ct)
    {
        // Pass 1 — base units of each category (BaseUnitId = null, ConversionFactor = 1)
        var baseUnits = new[]
        {
            new UnitOfMeasure { Code = "PCS", Name = "Pieces",   Symbol = "pcs", UnitType = UnitType.Count,  ConversionFactor = 1m, IsActive = true },
            new UnitOfMeasure { Code = "KG",  Name = "Kilogram", Symbol = "kg",  UnitType = UnitType.Weight, ConversionFactor = 1m, IsActive = true },
            new UnitOfMeasure { Code = "MTR", Name = "Meter",    Symbol = "m",   UnitType = UnitType.Length, ConversionFactor = 1m, IsActive = true },
            new UnitOfMeasure { Code = "LTR", Name = "Liter",    Symbol = "L",   UnitType = UnitType.Volume, ConversionFactor = 1m, IsActive = true },
        };

        foreach (var u in baseUnits)
        {
            var existing = await _db.UnitsOfMeasure
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Code == u.Code, ct);

            if (existing is null)
                _db.UnitsOfMeasure.Add(u);
            else if (existing.IsDeleted)
                Restore(existing);
        }
        await _db.SaveChangesAsync(ct);

        // Resolve base IDs for pass 2 FK assignment (include possibly-restored rows)
        var pcsId = (await _db.UnitsOfMeasure.IgnoreQueryFilters().SingleAsync(u => u.Code == "PCS", ct)).Id;
        var kgId  = (await _db.UnitsOfMeasure.IgnoreQueryFilters().SingleAsync(u => u.Code == "KG",  ct)).Id;
        var mtrId = (await _db.UnitsOfMeasure.IgnoreQueryFilters().SingleAsync(u => u.Code == "MTR", ct)).Id;
        var ltrId = (await _db.UnitsOfMeasure.IgnoreQueryFilters().SingleAsync(u => u.Code == "LTR", ct)).Id;

        // Pass 2 — derived units (1 of this = ConversionFactor base units)
        var derivatives = new[]
        {
            new UnitOfMeasure { Code = "DZN", Name = "Dozen",      Symbol = "dz",  UnitType = UnitType.Count,  BaseUnitId = pcsId, ConversionFactor = 12m,     IsActive = true },
            new UnitOfMeasure { Code = "BOX", Name = "Box",        Symbol = "box", UnitType = UnitType.Count,  BaseUnitId = pcsId, ConversionFactor = 100m,    IsActive = true },
            new UnitOfMeasure { Code = "GRM", Name = "Gram",       Symbol = "g",   UnitType = UnitType.Weight, BaseUnitId = kgId,  ConversionFactor = 0.001m,  IsActive = true },
            new UnitOfMeasure { Code = "YRD", Name = "Yard",       Symbol = "yd",  UnitType = UnitType.Length, BaseUnitId = mtrId, ConversionFactor = 0.9144m, IsActive = true },
            new UnitOfMeasure { Code = "INC", Name = "Inch",       Symbol = "in",  UnitType = UnitType.Length, BaseUnitId = mtrId, ConversionFactor = 0.0254m, IsActive = true },
            new UnitOfMeasure { Code = "ML",  Name = "Milliliter", Symbol = "ml",  UnitType = UnitType.Volume, BaseUnitId = ltrId, ConversionFactor = 0.001m,  IsActive = true },
        };

        foreach (var u in derivatives)
        {
            var existing = await _db.UnitsOfMeasure
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Code == u.Code, ct);

            if (existing is null)
                _db.UnitsOfMeasure.Add(u);
            else if (existing.IsDeleted)
                Restore(existing);
        }
        await _db.SaveChangesAsync(ct);
    }

    // ─── Warehouses ───────────────────────────────────────────────────────────

    private async Task SeedWarehousesAsync(CancellationToken ct)
    {
        var factory = await _db.Factories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.Code == "HQ", ct);
        if (factory is null) return;

        var warehouses = new[]
        {
            new Warehouse { Code = "MAIN",   Name = "Main Warehouse",       WarehouseType = WarehouseType.General,        FactoryId = factory.Id, IsActive = true },
            new Warehouse { Code = "RM",     Name = "Raw Material Store",   WarehouseType = WarehouseType.RawMaterial,    FactoryId = factory.Id, IsActive = true },
            new Warehouse { Code = "FG",     Name = "Finished Goods Store", WarehouseType = WarehouseType.FinishedGoods,  FactoryId = factory.Id, IsActive = true },
            new Warehouse { Code = "WIP",    Name = "Work-in-Progress",     WarehouseType = WarehouseType.WorkInProgress, FactoryId = factory.Id, IsActive = true },
            new Warehouse { Code = "REJECT", Name = "Reject/Damage Store",  WarehouseType = WarehouseType.Reject,         FactoryId = factory.Id, IsActive = true },
        };

        foreach (var w in warehouses)
        {
            var existing = await _db.Warehouses
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.FactoryId == w.FactoryId && x.Code == w.Code, ct);

            if (existing is null)
                _db.Warehouses.Add(w);
            else if (existing.IsDeleted)
                Restore(existing);
        }
        await _db.SaveChangesAsync(ct);
    }

    // ─── Product Categories ───────────────────────────────────────────────────

    private async Task SeedProductCategoriesAsync(CancellationToken ct)
    {
        var categories = new[]
        {
            new ProductCategory { Code = "WLBL", Name = "Woven Labels",  Description = "Woven garment labels", IsActive = true },
            new ProductCategory { Code = "TAG",  Name = "Hand Tags",     Description = "Hang tags and price tags", IsActive = true },
            new ProductCategory { Code = "STKR", Name = "Stickers",      Description = "Barcode and poly bag stickers", IsActive = true },
            new ProductCategory { Code = "PKG",  Name = "Packaging",     Description = "Poly bags, cartons and packaging", IsActive = true },
        };

        foreach (var c in categories)
        {
            var existing = await _db.ProductCategories
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Code == c.Code, ct);

            if (existing is null)
                _db.ProductCategories.Add(c);
            else if (existing.IsDeleted)
                Restore(existing);
        }
        await _db.SaveChangesAsync(ct);
    }

    // ─── Super Admin User ─────────────────────────────────────────────────────

    private async Task SeedSuperAdminAsync(CancellationToken ct)
    {
        const string email = "admin@bengaltex.com";
        const string password = "Admin@123456";

        // Resolve HQ Factory for FactoryId assignment
        var hqFactoryId = (await _db.Factories
            .IgnoreQueryFilters()
            .Where(f => f.Code == "HQ")
            .Select(f => (int?)f.Id)
            .FirstOrDefaultAsync(ct));

        var existing = await _userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            // Backfill FactoryId if missing (idempotent — handles upgrade path)
            if (existing.FactoryId is null && hqFactoryId.HasValue)
            {
                existing.FactoryId = hqFactoryId;
                await _userManager.UpdateAsync(existing);
            }
            return;
        }

        var user = new ApplicationUser
        {
            UserName = "superadmin",
            Email = email,
            FullName = "Super Admin",
            FactoryId = hqFactoryId,
            IsActive = true,
            EmailConfirmed = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "system"
        };

        var result = await _userManager.CreateAsync(user, password);
        if (result.Succeeded)
            await _userManager.AddToRoleAsync(user, "SuperAdmin");
    }

    // ─── Numbering Series ─────────────────────────────────────────────────────

    private async Task SeedNumberingSeriesAsync(CancellationToken ct)
    {
        var year = DateTimeOffset.UtcNow.Year;

        var series = new[]
        {
            new NumberingSeries { Code = "SO",  Description = "Sales Order",        Prefix = "BTX/SO",  Separator = "/", IncludeYear = true, PaddingLength = 5, ResetCycle = ResetCycle.Yearly, CurrentYear = year },
            new NumberingSeries { Code = "PO",  Description = "Purchase Order",     Prefix = "BTX/PO",  Separator = "/", IncludeYear = true, PaddingLength = 5, ResetCycle = ResetCycle.Yearly, CurrentYear = year },
            new NumberingSeries { Code = "INV", Description = "Invoice",            Prefix = "BTX/INV", Separator = "/", IncludeYear = true, PaddingLength = 5, ResetCycle = ResetCycle.Yearly, CurrentYear = year },
            new NumberingSeries { Code = "QTN", Description = "Quotation",          Prefix = "BTX/QTN", Separator = "/", IncludeYear = true, PaddingLength = 5, ResetCycle = ResetCycle.Yearly, CurrentYear = year },
            new NumberingSeries { Code = "PRD", Description = "Production Order",   Prefix = "BTX/PRD", Separator = "/", IncludeYear = true, PaddingLength = 5, ResetCycle = ResetCycle.Yearly, CurrentYear = year },
            new NumberingSeries { Code = "GRN", Description = "Goods Receive Note", Prefix = "BTX/GRN", Separator = "/", IncludeYear = true, PaddingLength = 5, ResetCycle = ResetCycle.Yearly, CurrentYear = year },
            new NumberingSeries { Code = "PAY", Description = "Payment",            Prefix = "BTX/PAY", Separator = "/", IncludeYear = true, PaddingLength = 5, ResetCycle = ResetCycle.Yearly, CurrentYear = year },
            new NumberingSeries { Code = "CUST", Description = "Customer Code",     Prefix = "BTX/CUST", Separator = "/", IncludeYear = true, PaddingLength = 5, ResetCycle = ResetCycle.Yearly, CurrentYear = year },
            new NumberingSeries { Code = "SUPP", Description = "Supplier Code",     Prefix = "BTX/SUPP", Separator = "/", IncludeYear = true, PaddingLength = 5, ResetCycle = ResetCycle.Yearly, CurrentYear = year },
            new NumberingSeries { Code = "PROD", Description = "Product Code",      Prefix = "BTX/PROD", Separator = "/", IncludeYear = true, PaddingLength = 5, ResetCycle = ResetCycle.Yearly, CurrentYear = year },
            new NumberingSeries { Code = "RM",   Description = "Raw Material Code", Prefix = "BTX/RM",   Separator = "/", IncludeYear = true, PaddingLength = 5, ResetCycle = ResetCycle.Yearly, CurrentYear = year },
            new NumberingSeries { Code = "BOM",  Description = "Bill of Materials", Prefix = "BTX/BOM",  Separator = "/", IncludeYear = true, PaddingLength = 5, ResetCycle = ResetCycle.Yearly, CurrentYear = year },
            new NumberingSeries { Code = "MV",   Description = "Stock Movement",    Prefix = "BTX/MV",   Separator = "/", IncludeYear = true, PaddingLength = 5, ResetCycle = ResetCycle.Yearly, CurrentYear = year },
            new NumberingSeries { Code = "ADJ",  Description = "Stock Adjustment",  Prefix = "BTX/ADJ",  Separator = "/", IncludeYear = true, PaddingLength = 5, ResetCycle = ResetCycle.Yearly, CurrentYear = year },
            new NumberingSeries { Code = "DN",   Description = "Delivery Note",     Prefix = "BTX/DN",   Separator = "/", IncludeYear = true, PaddingLength = 5, ResetCycle = ResetCycle.Yearly, CurrentYear = year },
            new NumberingSeries { Code = "RCT",  Description = "Receipt",           Prefix = "BTX/RCT",  Separator = "/", IncludeYear = true, PaddingLength = 5, ResetCycle = ResetCycle.Yearly, CurrentYear = year },
            new NumberingSeries { Code = "SINV", Description = "Supplier Invoice",  Prefix = "BTX/SINV", Separator = "/", IncludeYear = true, PaddingLength = 5, ResetCycle = ResetCycle.Yearly, CurrentYear = year },
            new NumberingSeries { Code = "TXFR", Description = "Stock Transfer",    Prefix = "BTX/TXFR", Separator = "/", IncludeYear = true, PaddingLength = 5, ResetCycle = ResetCycle.Yearly, CurrentYear = year },
            new NumberingSeries { Code = "VC",   Description = "VAT Challan",       Prefix = "BTX/VC",   Separator = "/", IncludeYear = true, PaddingLength = 5, ResetCycle = ResetCycle.Yearly, CurrentYear = year },
            new NumberingSeries { Code = "CRN",  Description = "Customer Return",   Prefix = "BTX/CRN",  Separator = "/", IncludeYear = true, PaddingLength = 5, ResetCycle = ResetCycle.Yearly, CurrentYear = year },
            new NumberingSeries { Code = "SRN",  Description = "Supplier Return",   Prefix = "BTX/SRN",  Separator = "/", IncludeYear = true, PaddingLength = 5, ResetCycle = ResetCycle.Yearly, CurrentYear = year },
            new NumberingSeries { Code = "QC",   Description = "QC Inspection",     Prefix = "BTX/QC",   Separator = "/", IncludeYear = true, PaddingLength = 5, ResetCycle = ResetCycle.Yearly, CurrentYear = year },
            new NumberingSeries { Code = "DISP", Description = "Quarantine Disposition", Prefix = "BTX/DISP", Separator = "/", IncludeYear = true, PaddingLength = 5, ResetCycle = ResetCycle.Yearly, CurrentYear = year },
            new NumberingSeries { Code = "EMP",  Description = "Employee Code",       Prefix = "BTX/EMP",  Separator = "/", IncludeYear = true, PaddingLength = 5, ResetCycle = ResetCycle.Yearly, CurrentYear = year },
            new NumberingSeries { Code = "SUB",  Description = "Subcontract Order",   Prefix = "BTX/SUB",  Separator = "/", IncludeYear = true, PaddingLength = 5, ResetCycle = ResetCycle.Yearly, CurrentYear = year },
            new NumberingSeries { Code = "STY",  Description = "Style Code",          Prefix = "BTX/STY",  Separator = "/", IncludeYear = true, PaddingLength = 5, ResetCycle = ResetCycle.Yearly, CurrentYear = year },
            new NumberingSeries { Code = "LC",   Description = "Letter of Credit",    Prefix = "BTX/LC",   Separator = "/", IncludeYear = true, PaddingLength = 5, ResetCycle = ResetCycle.Yearly, CurrentYear = year },
            new NumberingSeries { Code = "LOT",  Description = "Stock Lot / Batch",   Prefix = "BTX/LOT",  Separator = "/", IncludeYear = true, PaddingLength = 5, ResetCycle = ResetCycle.Yearly, CurrentYear = year },
            new NumberingSeries { Code = "JV",   Description = "Journal Voucher",     Prefix = "BTX/JV",   Separator = "/", IncludeYear = true, PaddingLength = 5, ResetCycle = ResetCycle.Yearly, CurrentYear = year },
            new NumberingSeries { Code = "EXP",  Description = "Expense Voucher",     Prefix = "BTX/EXP",  Separator = "/", IncludeYear = true, PaddingLength = 5, ResetCycle = ResetCycle.Yearly, CurrentYear = year },
            new NumberingSeries { Code = "QUOT", Description = "Quotation",           Prefix = "BTX/QUOT", Separator = "/", IncludeYear = true, PaddingLength = 5, ResetCycle = ResetCycle.Yearly, CurrentYear = year },
            new NumberingSeries { Code = "SMP",  Description = "Sample",              Prefix = "BTX/SMP",  Separator = "/", IncludeYear = true, PaddingLength = 5, ResetCycle = ResetCycle.Yearly, CurrentYear = year },
            new NumberingSeries { Code = "WST",  Description = "Wastage Entry",       Prefix = "BTX/WST",  Separator = "/", IncludeYear = true, PaddingLength = 5, ResetCycle = ResetCycle.Yearly, CurrentYear = year },
        };

        foreach (var s in series)
        {
            var exists = await _db.NumberingSeries
                .AnyAsync(n => n.Code == s.Code && n.FactoryId == null, ct);
            if (!exists)
                _db.NumberingSeries.Add(s);
        }

        await _db.SaveChangesAsync(ct);
    }
}
