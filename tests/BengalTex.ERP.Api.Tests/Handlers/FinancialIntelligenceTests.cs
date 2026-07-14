using BengalTex.ERP.Api.Tests.TestSupport;
using BengalTex.ERP.Application.Accounting.Dtos;
using BengalTex.ERP.Application.Accounting.Intelligence;
using BengalTex.ERP.Application.Accounting.Queries;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Infrastructure.Persistence;
using BengalTex.ERP.Infrastructure.Persistence.Repositories;
using BengalTex.ERP.Shared.Common;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace BengalTex.ERP.Api.Tests.Handlers;

/// <summary>
/// Phase A8 — Financial Intelligence: ratios (over BS + P&amp;L), AR/AP aging buckets, and the P&amp;L
/// trend, all read-only over posted GL + open invoices.
/// </summary>
public class FinancialIntelligenceTests
{
    // ── I1 — ratios (BS/PL mocked to control inputs precisely) ──

    [Fact]
    public async Task Kpis_compute_liquidity_profitability_and_efficiency_ratios()
    {
        static StatementLineDto L(int id, string code, decimal amt) => new(id, code, code, amt);

        var bs = new BalanceSheetDto(new DateOnly(2026, 12, 31),
            new[] { L(1, "1120", 40_000m), L(2, "1130", 20_000m), L(3, "1140", 20_000m), L(4, "1150", 10_000m), L(5, "1210", 10_000m) },
            100_000m,
            new[] { L(6, "2110", 25_000m) }, 25_000m,
            Array.Empty<StatementLineDto>(), 75_000m, 75_000m, 100_000m, true);

        var pl = new ProfitAndLossDto(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31),
            Array.Empty<StatementLineDto>(), 100_000m,
            new[] { L(7, "5100", 60_000m), L(8, "5400", 20_000m) }, 80_000m, 20_000m);

        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetBalanceSheetQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(ApiResponse<BalanceSheetDto>.Ok(bs));
        mediator.Setup(m => m.Send(It.IsAny<GetProfitAndLossQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(ApiResponse<ProfitAndLossDto>.Ok(pl));

        var res = await new GetFinancialKpisQueryHandler(mediator.Object)
            .Handle(new GetFinancialKpisQuery(new DateOnly(2026, 12, 31), new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)), default);

        res.Success.Should().BeTrue();
        var k = res.Data!;
        k.CurrentAssets.Should().Be(90_000m);   // 11xx = 40k+20k+20k+10k
        k.Inventory.Should().Be(30_000m);        // 1140 + 1150
        k.CurrentRatio.Should().Be(3.6m);        // 90,000 / 25,000
        k.QuickRatio.Should().Be(2.4m);          // (90,000 − 30,000) / 25,000
        k.GrossMarginPct.Should().Be(40m);       // (100k − 60k) / 100k
        k.NetMarginPct.Should().Be(20m);
        k.Dso.Should().Be(73m);                  // 20,000 / 100,000 × 365
        k.InventoryTurnover.Should().Be(2m);     // 60,000 / 30,000 (annualised over the full year)
    }

    // ── I2 — AR aging buckets ──

    [Fact]
    public async Task Ar_aging_buckets_invoices_by_age()
    {
        await using var ctx = TestHarness.NewContext();
        ctx.Customers.Add(new Customer { Id = 1, Code = "C1", Name = "Acme Buyer" });
        var asOf = new DateOnly(2026, 6, 30);
        ctx.CustomerInvoices.AddRange(
            new CustomerInvoice { Code = "INV-1", CustomerId = 1, CurrencyId = 1, ExchangeRate = 1m, TotalAmount = 1_000m, AmountPaid = 0m, Status = CustomerInvoiceStatus.Issued, InvoiceDate = new DateOnly(2026, 6, 20) },   // age 10 → 0-30
            new CustomerInvoice { Code = "INV-2", CustomerId = 1, CurrencyId = 1, ExchangeRate = 1m, TotalAmount = 2_000m, AmountPaid = 0m, Status = CustomerInvoiceStatus.Issued, InvoiceDate = new DateOnly(2026, 5, 1) },    // age 60 → 31-60
            new CustomerInvoice { Code = "INV-3", CustomerId = 1, CurrencyId = 1, ExchangeRate = 1m, TotalAmount = 500m,  AmountPaid = 0m, Status = CustomerInvoiceStatus.Issued, InvoiceDate = new DateOnly(2026, 1, 1) });    // age 180 → 90+
        ctx.SaveChanges();

        var res = await new GetArApAgingQueryHandler(new Repository<CustomerInvoice, long>(ctx), new Repository<SupplierInvoice, long>(ctx))
            .Handle(new GetArApAgingQuery(asOf), default);

        res.Success.Should().BeTrue();
        var ar = res.Data!.Receivables;
        var row = ar.Rows.Single(r => r.Party == "Acme Buyer");
        row.Bucket0_30.Should().Be(1_000m);
        row.Bucket31_60.Should().Be(2_000m);
        row.Bucket90Plus.Should().Be(500m);
        row.Total.Should().Be(3_500m);
        ar.GrandTotal.Should().Be(3_500m);
        res.Data.Payables.GrandTotal.Should().Be(0m);
    }

    // ── I3 — P&L trend ──

    [Fact]
    public async Task Profit_trend_reports_monthly_revenue_expense_net()
    {
        await using var ctx = TestHarness.NewContext();
        ctx.Accounts.AddRange(
            new Account { Id = 1, Code = "4100", Name = "Sales", AccountType = AccountType.Income },
            new Account { Id = 2, Code = "5400", Name = "Admin", AccountType = AccountType.Expense });
        var now = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthStart = new DateOnly(now.Year, now.Month, 10);
        var je = new JournalEntry { Code = "JV1", EntryDate = monthStart, Status = JournalEntryStatus.Posted, VoucherType = VoucherType.Journal, PostedAt = DateTimeOffset.UtcNow, PostedBy = "t" };
        je.Lines.Add(new JournalEntryLine { AccountId = 1, Debit = 0m, Credit = 5_000m, SortOrder = 0 });   // revenue
        je.Lines.Add(new JournalEntryLine { AccountId = 2, Debit = 3_000m, Credit = 0m, SortOrder = 1 });   // expense
        ctx.JournalEntries.Add(je);
        ctx.SaveChanges();

        var res = await new GetProfitTrendQueryHandler(new Repository<JournalEntryLine, long>(ctx))
            .Handle(new GetProfitTrendQuery(12), default);

        res.Success.Should().BeTrue();
        res.Data!.Points.Should().HaveCount(12);
        var last = res.Data.Points[^1];   // current month
        last.Revenue.Should().Be(5_000m);
        last.Expense.Should().Be(3_000m);
        last.NetProfit.Should().Be(2_000m);
    }
}
