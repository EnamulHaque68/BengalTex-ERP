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
        await SeedSuperAdminAsync(ct);
        await SeedNumberingSeriesAsync(ct);
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
            p == Permissions.Customers.View ||
            p == Permissions.Suppliers.View));

        await AssignPermissionsAsync("HRManager", all.Where(p =>
            p.StartsWith("Employees.") ||
            p.StartsWith("Attendance.") ||
            p == Permissions.Reports.ViewHr ||
            p == Permissions.Reports.Export ||
            p == Permissions.Dashboard.ViewHr));

        await AssignPermissionsAsync("ProductionManager", all.Where(p =>
            p.StartsWith("Production.") ||
            p.StartsWith("Inventory.") ||
            p.StartsWith("Boms.") ||
            p.StartsWith("RawMaterials.") ||
            p == Permissions.Reports.ViewProduction ||
            p == Permissions.Reports.ViewInventory ||
            p == Permissions.Reports.Export ||
            p == Permissions.Dashboard.ViewProduction ||
            p == Permissions.Products.View));

        await AssignPermissionsAsync("SalesManager", all.Where(p =>
            p.StartsWith("SalesOrders.") ||
            p.StartsWith("Customers.") ||
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

    // ─── Company ──────────────────────────────────────────────────────────────

    private async Task SeedCompanyAsync(CancellationToken ct)
    {
        if (await _db.Companies.AnyAsync(ct)) return;

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
        if (await _db.Factories.AnyAsync(f => f.Code == "HQ", ct)) return;

        var company = await _db.Companies.FirstOrDefaultAsync(ct);
        if (company is null) return; // Defensive — SeedCompanyAsync should have created one

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
            if (!await _db.Currencies.AnyAsync(x => x.Code == c.Code, ct))
                _db.Currencies.Add(c);
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
            if (!await _db.UnitsOfMeasure.AnyAsync(x => x.Code == u.Code, ct))
                _db.UnitsOfMeasure.Add(u);
        }
        await _db.SaveChangesAsync(ct);

        // Resolve base IDs for pass 2 FK assignment
        var pcsId = (await _db.UnitsOfMeasure.SingleAsync(u => u.Code == "PCS", ct)).Id;
        var kgId  = (await _db.UnitsOfMeasure.SingleAsync(u => u.Code == "KG",  ct)).Id;
        var mtrId = (await _db.UnitsOfMeasure.SingleAsync(u => u.Code == "MTR", ct)).Id;
        var ltrId = (await _db.UnitsOfMeasure.SingleAsync(u => u.Code == "LTR", ct)).Id;

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
            if (!await _db.UnitsOfMeasure.AnyAsync(x => x.Code == u.Code, ct))
                _db.UnitsOfMeasure.Add(u);
        }
        await _db.SaveChangesAsync(ct);
    }

    // ─── Warehouses ───────────────────────────────────────────────────────────

    private async Task SeedWarehousesAsync(CancellationToken ct)
    {
        var factory = await _db.Factories.FirstOrDefaultAsync(f => f.Code == "HQ", ct);
        if (factory is null) return; // Defensive — SeedFactoryAsync should have created one

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
            if (!await _db.Warehouses.AnyAsync(x => x.FactoryId == w.FactoryId && x.Code == w.Code, ct))
                _db.Warehouses.Add(w);
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
