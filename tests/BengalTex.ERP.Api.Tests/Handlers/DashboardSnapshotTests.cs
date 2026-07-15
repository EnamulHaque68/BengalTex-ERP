using BengalTex.ERP.Api.Tests.TestSupport;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Dashboard.Queries;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Infrastructure.Persistence;
using BengalTex.ERP.Infrastructure.Persistence.Repositories;
using BengalTex.ERP.Shared.Permissions;
using FluentAssertions;
using Xunit;

namespace BengalTex.ERP.Api.Tests.Handlers;

/// <summary>
/// Redesigned dashboard snapshot — verifies the new read-only KPIs (today sales/purchase/expense,
/// RM/FG split, overdue AR, low-stock, expense breakdown) and that every section stays gated by the
/// caller's <c>Dashboard.*</c> permissions (no unauthorized data ever leaves the handler).
/// </summary>
public class DashboardSnapshotTests
{
    private sealed class PermUser : ICurrentUserService
    {
        private readonly HashSet<string> _perms;
        public PermUser(params string[] perms) => _perms = new(perms);
        public string? UserId => "u"; public string? UserName => "Tester";
        public int? FactoryId => null; public string? IpAddress => null; public string? UserAgent => null;
        public bool IsAuthenticated => true;
        public IReadOnlyList<string> Roles => System.Array.Empty<string>();
        public bool IsInRole(string role) => false;
        public bool HasPermission(string permission) => _perms.Contains(permission);
        public IReadOnlyList<string> Permissions => _perms.ToList();
    }

    private static GetDashboardSnapshotQueryHandler Handler(ApplicationDbContext ctx, ICurrentUserService user) =>
        new(new Repository<JournalEntryLine, long>(ctx), new Repository<Account>(ctx), new Repository<StockOnHand>(ctx),
            new Repository<CustomerInvoice, long>(ctx), new Repository<SupplierInvoice, long>(ctx),
            new Repository<Quotation, long>(ctx), new Repository<SalesOrder, long>(ctx), new Repository<PurchaseOrder, long>(ctx),
            new Repository<GoodsReceiptNote, long>(ctx), new Repository<Payment, long>(ctx),
            new Repository<ProductionOrder, long>(ctx), new Repository<JobCard, long>(ctx), new Repository<WastageEntry, long>(ctx),
            new Repository<MachineMaintenance, long>(ctx), new Repository<Employee>(ctx), new Repository<AttendanceRecord, long>(ctx),
            new Repository<LeaveApplication, long>(ctx), new Repository<EmployeeLoan, long>(ctx), new Repository<BankStatement, long>(ctx),
            new Repository<JournalEntry, long>(ctx), new Repository<ComplianceCertificate>(ctx), new Repository<ComplianceAudit, long>(ctx),
            new Repository<AuditFinding, long>(ctx), user, new StubClock());

    private static void Seed(ApplicationDbContext ctx)
    {
        var today = new DateOnly(2026, 5, 22);   // matches StubClock
        ctx.Accounts.AddRange(
            new Account { Id = 1, Code = "1110", Name = "Cash", AccountType = AccountType.Asset },
            new Account { Id = 2, Code = "1120", Name = "Bank", AccountType = AccountType.Asset },
            new Account { Id = 3, Code = "5400", Name = "Admin Expense", AccountType = AccountType.Expense });
        ctx.CustomerInvoices.AddRange(
            new CustomerInvoice { Code = "INV-1", CustomerId = 1, CurrencyId = 1, ExchangeRate = 1m, TotalAmount = 5_000m, AmountPaid = 0m, Status = CustomerInvoiceStatus.Issued, InvoiceDate = today, DueDate = today.AddDays(15) },
            new CustomerInvoice { Code = "INV-2", CustomerId = 1, CurrencyId = 1, ExchangeRate = 1m, TotalAmount = 2_000m, AmountPaid = 0m, Status = CustomerInvoiceStatus.Issued, InvoiceDate = today.AddDays(-40), DueDate = today.AddDays(-10) }); // overdue
        ctx.SupplierInvoices.Add(
            new SupplierInvoice { Code = "SINV-1", SupplierId = 1, CurrencyId = 1, ExchangeRate = 1m, TotalAmount = 3_000m, AmountPaid = 0m, Status = SupplierInvoiceStatus.Approved, InvoiceDate = today });
        var je = new JournalEntry { Code = "JV1", EntryDate = today, Status = JournalEntryStatus.Posted, VoucherType = VoucherType.Journal, PostedAt = System.DateTimeOffset.UtcNow, PostedBy = "t" };
        je.Lines.Add(new JournalEntryLine { AccountId = 3, Debit = 2_000m, Credit = 0m, SortOrder = 0 });
        ctx.JournalEntries.Add(je);
        // Low-stock raw material
        ctx.UnitsOfMeasure.Add(new UnitOfMeasure { Id = 1, Code = "KG", Name = "Kilogram" });
        ctx.RawMaterials.Add(new RawMaterial { Id = 1, Code = "RM-1", Name = "Cotton Yarn", UnitOfMeasureId = 1, MinimumStockLevel = 100m, WeightedAverageCost = 10m });
        ctx.StockOnHand.Add(new StockOnHand { RawMaterialId = 1, WarehouseId = 1, Quantity = 20m });   // 20 ≤ 100 → Critical (< 50)
        ctx.Employees.Add(new Employee { Id = 1, Code = "E1", FullName = "Karim", BasicSalary = 30_000m, IsActive = true, Status = EmployeeStatus.Active, EmploymentType = EmploymentType.Permanent, Gender = Gender.Male, JoiningDate = today.AddYears(-1) });
        ctx.AttendanceRecords.Add(new AttendanceRecord { EmployeeId = 1, AttendanceDate = today, Status = AttendanceStatus.Present });
        ctx.SaveChanges();
    }

    [Fact]
    public async Task Full_permission_user_gets_all_new_kpis()
    {
        await using var ctx = TestHarness.NewContext();
        Seed(ctx);
        var user = new PermUser(Permissions.Dashboard.ViewOwner, Permissions.Dashboard.ViewSales,
            Permissions.Dashboard.ViewProduction, Permissions.Dashboard.ViewAccounts, Permissions.Dashboard.ViewHr);

        var res = await Handler(ctx, user).Handle(new GetDashboardSnapshotQuery(), default);

        res.Success.Should().BeTrue();
        var d = res.Data!;
        d.TodayKpis.Should().NotBeNull();
        d.TodayKpis!.TodaySales.Should().Be(5_000m);
        d.TodayKpis.TodayPurchase.Should().Be(3_000m);
        d.TodayKpis.TodayExpenses.Should().Be(2_000m);
        d.TodayKpis.SalesSpark.Should().HaveCount(7);
        d.Hero.OverdueArAmount.Should().Be(2_000m);
        d.Hero.OverdueArCount.Should().Be(1);
        d.Hero.RawMaterialStockValue.Should().Be(200m);      // 20 × WAC 10
        d.ExpenseBreakdown.Should().NotBeNull().And.Contain(x => x.Name == "Admin Expense");
        d.LowStock.Should().NotBeNull().And.ContainSingle(x => x.ItemName == "Cotton Yarn" && x.Status == "Critical");
        d.Hr.Should().NotBeNull();
        d.Hr!.Attendance.Should().NotBeNull();
        d.Hr.UpcomingSalary.Should().NotBeNull();
        d.Production.Should().NotBeNull();
    }

    [Fact]
    public async Task User_without_dashboard_permissions_gets_gated_nulls_only()
    {
        await using var ctx = TestHarness.NewContext();
        Seed(ctx);
        var user = new PermUser();   // no permissions

        var res = await Handler(ctx, user).Handle(new GetDashboardSnapshotQuery(), default);

        res.Success.Should().BeTrue();
        var d = res.Data!;
        // Hero (non-sensitive summary) is still there…
        d.Hero.Should().NotBeNull();
        // …but every gated block is null — no unauthorized data leaves the handler.
        d.TodayKpis.Should().BeNull();
        d.ExpenseBreakdown.Should().BeNull();
        d.LowStock.Should().BeNull();
        d.Sales.Should().BeNull();
        d.Production.Should().BeNull();
        d.Hr.Should().BeNull();
        d.Accounting.Should().BeNull();
    }
}
