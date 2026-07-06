using BengalTex.ERP.Api.Tests.TestSupport;
using BengalTex.ERP.Application.Accounting.Dimensions;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Infrastructure.Persistence;
using BengalTex.ERP.Infrastructure.Persistence.Repositories;
using BengalTex.ERP.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BengalTex.ERP.Api.Tests.Handlers;

/// <summary>
/// Phase A3 — Dimensions &amp; Profitability. Covers the dimensioned posting engine (stamping +
/// RequiresCostCenter guard), the cost-center master, and the buyer/style profitability +
/// cost-center reports over dimensioned journal lines.
/// </summary>
public class DimensionsTests
{
    private static void SeedCoa(ApplicationDbContext ctx)
    {
        ctx.Accounts.AddRange(
            new Account { Id = 1, Code = "1130", Name = "AR", AccountType = AccountType.Asset },
            new Account { Id = 2, Code = "4100", Name = "Sales", AccountType = AccountType.Income },
            new Account { Id = 3, Code = "5100", Name = "COGS", AccountType = AccountType.Expense },
            new Account { Id = 4, Code = "1150", Name = "FG", AccountType = AccountType.Asset },
            new Account { Id = 5, Code = "5200", Name = "Salary", AccountType = AccountType.Expense, RequiresCostCenter = true });
    }

    private static JournalPostingService Posting(ApplicationDbContext ctx) =>
        new(new Repository<JournalEntry, long>(ctx), new Repository<Account>(ctx),
            TestHarness.Numbering().Object, new StubCurrentUser(), new StubClock(),
            new PeriodGuard(ctx, new StubCurrentUser()));

    // ── 1. Dimensioned posting stamps the tags ──

    [Fact]
    public async Task Posting_stamps_dimensions_onto_lines()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoa(ctx);
        ctx.SaveChanges();

        await Posting(ctx).PostAsync(new DateOnly(2026, 3, 1), "sale", "CustomerInvoice", 1, "INV-1",
            new[]
            {
                new JournalPostingLine("1130", 100m, 0m, new Dimensions(BuyerId: 7, SalesOrderId: 55)),
                new JournalPostingLine("4100", 0m, 100m, new Dimensions(BuyerId: 7, StyleId: 9, SalesOrderId: 55)),
            });
        ctx.SaveChanges();

        var sales = ctx.JournalEntryLines.Single(l => l.AccountId == 2);
        sales.BuyerId.Should().Be(7);
        sales.StyleId.Should().Be(9);
        sales.SalesOrderId.Should().Be(55);
        var ar = ctx.JournalEntryLines.Single(l => l.AccountId == 1);
        ar.BuyerId.Should().Be(7);
        ar.StyleId.Should().BeNull();   // AR carries no style
    }

    // ── 2. RequiresCostCenter guard ──

    [Fact]
    public async Task Posting_to_a_required_cost_center_account_without_one_is_rejected()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoa(ctx);
        ctx.SaveChanges();

        var act = () => Posting(ctx).PostAsync(new DateOnly(2026, 3, 1), "salary", "Payslip", 1, "PS-1",
            new[]
            {
                new JournalPostingLine("5200", 5000m, 0m),      // no cost center → must fail
                new JournalPostingLine("1130", 0m, 5000m),
            });

        await act.Should().ThrowAsync<FluentValidation.ValidationException>()
            .Where(e => e.Message.Contains("requires a cost center"));
        ctx.JournalEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task Posting_to_a_required_cost_center_account_with_one_succeeds()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoa(ctx);
        ctx.SaveChanges();

        await Posting(ctx).PostAsync(new DateOnly(2026, 3, 1), "salary", "Payslip", 1, "PS-1",
            new[]
            {
                new JournalPostingLine("5200", 5000m, 0m, new Dimensions(CostCenterId: 3)),
                new JournalPostingLine("1130", 0m, 5000m),
            });
        ctx.SaveChanges();

        ctx.JournalEntryLines.Single(l => l.AccountId == 5).CostCenterId.Should().Be(3);
    }

    // ── 3. Cost-center master ──

    [Fact]
    public async Task Cost_center_create_rejects_duplicate_code()
    {
        await using var ctx = TestHarness.NewContext();
        var handler = new CreateCostCenterCommandHandler(new Repository<CostCenter>(ctx), new UnitOfWork(ctx));

        (await handler.Handle(new CreateCostCenterCommand("CUT", "Cutting", "Cost", null, null, null, null), default))
            .Success.Should().BeTrue();
        var dup = await handler.Handle(new CreateCostCenterCommand("CUT", "Cutting 2", "Cost", null, null, null, null), default);
        dup.Success.Should().BeFalse();
        dup.Message.Should().Contain("already exists");
    }

    // ── 4. Buyer & style profitability ──

    private static JournalEntry PlLine(int accountId, decimal debit, decimal credit, int? buyer, int? style, long? order, DateOnly date) => new()
    {
        Code = "JV", EntryDate = date, Status = JournalEntryStatus.Posted, PostedAt = DateTimeOffset.UtcNow, PostedBy = "t",
        Lines = { new JournalEntryLine { AccountId = accountId, Debit = debit, Credit = credit, BuyerId = buyer, StyleId = style, SalesOrderId = order, SortOrder = 0 } }
    };

    private GetProfitabilityReportQueryHandler ProfitHandler(ApplicationDbContext ctx) =>
        new(new Repository<JournalEntryLine, long>(ctx), new Repository<Customer>(ctx),
            new Repository<Style>(ctx), new Repository<Domain.Entities.SalesOrder, long>(ctx));

    [Fact]
    public async Task Buyer_profitability_computes_revenue_minus_cogs()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoa(ctx);
        ctx.Customers.Add(new Customer { Id = 7, Code = "B1", Name = "Zara" });
        // Buyer 7: revenue 1000 (Cr 4100), COGS 600 (Dr 5100) → GP 400, margin 40%.
        ctx.JournalEntries.Add(PlLine(2, 0m, 1000m, buyer: 7, style: null, order: 55, new DateOnly(2026, 3, 5)));
        ctx.JournalEntries.Add(PlLine(3, 600m, 0m, buyer: 7, style: null, order: 55, new DateOnly(2026, 3, 6)));
        ctx.SaveChanges();

        var res = await ProfitHandler(ctx).Handle(
            new GetProfitabilityReportQuery(ProfitDimension.Buyer, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31)), default);

        res.Success.Should().BeTrue();
        var row = res.Data!.Rows.Single();
        row.DimensionName.Should().Be("Zara");
        row.Revenue.Should().Be(1000m);
        row.Cogs.Should().Be(600m);
        row.GrossProfit.Should().Be(400m);
        row.MarginPercent.Should().Be(40m);
        res.Data.TotalGrossProfit.Should().Be(400m);
    }

    [Fact]
    public async Task Style_profitability_splits_by_style()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoa(ctx);
        ctx.Styles.Add(new Style { Id = 9, Code = "STY-1", StyleName = "Woven A", BuyerId = 7 });
        ctx.Styles.Add(new Style { Id = 10, Code = "STY-2", StyleName = "Hangtag B", BuyerId = 7 });
        ctx.JournalEntries.Add(PlLine(2, 0m, 500m, 7, style: 9, order: 55, new DateOnly(2026, 3, 5)));
        ctx.JournalEntries.Add(PlLine(3, 300m, 0m, 7, style: 9, order: 55, new DateOnly(2026, 3, 6)));
        ctx.JournalEntries.Add(PlLine(2, 0m, 200m, 7, style: 10, order: 55, new DateOnly(2026, 3, 7)));
        ctx.SaveChanges();

        var res = await ProfitHandler(ctx).Handle(
            new GetProfitabilityReportQuery(ProfitDimension.Style, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31)), default);

        res.Success.Should().BeTrue();
        res.Data!.Rows.Should().HaveCount(2);
        res.Data.Rows.Single(r => r.DimensionId == 9).GrossProfit.Should().Be(200m);   // 500 − 300
        res.Data.Rows.Single(r => r.DimensionId == 10).GrossProfit.Should().Be(200m);  // 200 − 0
    }

    // ── 5. Cost-center statement ──

    [Fact]
    public async Task Cost_center_statement_sums_income_and_expense()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoa(ctx);
        ctx.CostCenters.Add(new CostCenter { Id = 3, Code = "ADM", Name = "Admin", Kind = CostCenterKind.Cost });
        // CC 3: expense 5000 on the salary account.
        var je = new JournalEntry { Code = "JV", EntryDate = new DateOnly(2026, 3, 1), Status = JournalEntryStatus.Posted, PostedAt = DateTimeOffset.UtcNow, PostedBy = "t",
            Lines = { new JournalEntryLine { AccountId = 5, Debit = 5000m, Credit = 0m, CostCenterId = 3, SortOrder = 0 } } };
        ctx.JournalEntries.Add(je);
        ctx.SaveChanges();

        var res = await new GetCostCenterStatementQueryHandler(
                new Repository<JournalEntryLine, long>(ctx), new Repository<CostCenter>(ctx))
            .Handle(new GetCostCenterStatementQuery(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31)), default);

        res.Success.Should().BeTrue();
        var row = res.Data!.Rows.Single();
        row.CostCenterName.Should().Contain("Admin");
        row.Expense.Should().Be(5000m);
        row.Net.Should().Be(-5000m);
    }
}
