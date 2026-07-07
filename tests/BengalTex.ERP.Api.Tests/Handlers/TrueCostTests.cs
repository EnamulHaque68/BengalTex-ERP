using BengalTex.ERP.Api.Tests.TestSupport;
using BengalTex.ERP.Application.Accounting.InventoryGL;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Infrastructure.Persistence;
using BengalTex.ERP.Infrastructure.Persistence.Repositories;
using BengalTex.ERP.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace BengalTex.ERP.Api.Tests.Handlers;

/// <summary>
/// Phase A4 — True Cost (absorption). Covers the rate resolver, the month-end absorption true-up
/// (applied contra → 5165) and the auto-reversing WIP snapshot. The applied-cost legs at
/// production completion are exercised through the existing production-completion test path.
/// </summary>
public class TrueCostTests
{
    private static void SeedCoa(ApplicationDbContext ctx)
    {
        ctx.Accounts.AddRange(
            new Account { Id = 1, Code = "1160", Name = "WIP", AccountType = AccountType.Asset },
            new Account { Id = 2, Code = "2155", Name = "WIP Accrual", AccountType = AccountType.Liability },
            new Account { Id = 3, Code = "5165", Name = "Over/Under-Absorbed", AccountType = AccountType.Expense },
            new Account { Id = 4, Code = "5200", Name = "Salary", AccountType = AccountType.Expense },
            new Account { Id = 5, Code = "5211", Name = "Applied Labour", AccountType = AccountType.Expense },
            new Account { Id = 6, Code = "5300", Name = "Factory Overhead", AccountType = AccountType.Expense },
            new Account { Id = 7, Code = "5311", Name = "Applied FOH", AccountType = AccountType.Expense });
    }

    private static JournalEntry Posted(string code, DateOnly date, params (int acc, decimal dr, decimal cr)[] legs)
    {
        var je = new JournalEntry { Code = code, EntryDate = date, Status = JournalEntryStatus.Posted, PostedAt = DateTimeOffset.UtcNow, PostedBy = "t" };
        var i = 0;
        foreach (var (acc, dr, cr) in legs) je.Lines.Add(new JournalEntryLine { AccountId = acc, Debit = dr, Credit = cr, SortOrder = i++ });
        return je;
    }

    // ── 1. Rate resolver ──

    [Fact]
    public async Task Resolver_returns_zero_when_no_rate_configured()
    {
        await using var ctx = TestHarness.NewContext();
        var resolver = new CostingRateResolver(ctx);
        (await resolver.ResolveAsync(CostingRateType.Labour, new DateOnly(2026, 3, 1))).Should().Be(0m);
    }

    [Fact]
    public async Task Resolver_picks_latest_effective_and_prefers_work_center()
    {
        await using var ctx = TestHarness.NewContext();
        ctx.CostingRates.AddRange(
            new CostingRate { RateType = CostingRateType.Labour, Basis = CostingRateBasis.PerLabourMinute, Rate = 1.5m, EffectiveFrom = new DateOnly(2026, 1, 1), IsActive = true },
            new CostingRate { RateType = CostingRateType.Labour, Basis = CostingRateBasis.PerLabourMinute, Rate = 2.0m, EffectiveFrom = new DateOnly(2026, 3, 1), IsActive = true },   // latest global
            new CostingRate { RateType = CostingRateType.Labour, Basis = CostingRateBasis.PerLabourMinute, Rate = 3.0m, EffectiveFrom = new DateOnly(2026, 2, 1), WorkCenterId = 5, IsActive = true });  // WC-specific
        ctx.SaveChanges();
        var resolver = new CostingRateResolver(ctx);

        (await resolver.ResolveAsync(CostingRateType.Labour, new DateOnly(2026, 3, 15))).Should().Be(2.0m);        // latest global
        (await resolver.ResolveAsync(CostingRateType.Labour, new DateOnly(2026, 3, 15), workCenterId: 5)).Should().Be(3.0m);  // WC beats global
        (await resolver.ResolveAsync(CostingRateType.Labour, new DateOnly(2026, 1, 15))).Should().Be(1.5m);        // earlier date
        // A global rate exists → work with a *different* work center still falls back to it, not the WC-5 rate.
        (await resolver.ResolveAsync(CostingRateType.Labour, new DateOnly(2026, 3, 15), workCenterId: 9)).Should().Be(2.0m);
    }

    [Fact]
    public async Task Resolver_falls_back_to_a_work_center_rate_when_no_global_and_no_exact_match()
    {
        // Repro of the field bug: every rate was configured against one work center ("Cutting Line 1"),
        // but the job cards carried no routing stage (work center null) and factory-OH always resolves
        // with a null work center — so the rate must still apply instead of silently zeroing.
        await using var ctx = TestHarness.NewContext();
        ctx.CostingRates.AddRange(
            new CostingRate { RateType = CostingRateType.Labour, Basis = CostingRateBasis.PerLabourMinute, Rate = 2.0m, EffectiveFrom = new DateOnly(2026, 7, 1), WorkCenterId = 7, IsActive = true },
            new CostingRate { RateType = CostingRateType.FactoryOH, Basis = CostingRateBasis.PerUnit, Rate = 5.0m, EffectiveFrom = new DateOnly(2026, 7, 1), WorkCenterId = 7, IsActive = true });
        ctx.SaveChanges();
        var resolver = new CostingRateResolver(ctx);

        // Job card with no work center (null) — the WC-7 labour rate still resolves.
        (await resolver.ResolveAsync(CostingRateType.Labour, new DateOnly(2026, 7, 8), workCenterId: null)).Should().Be(2.0m);
        // Factory OH is always resolved plant-wide (null) — the WC-7 FOH rate still resolves.
        (await resolver.ResolveAsync(CostingRateType.FactoryOH, new DateOnly(2026, 7, 8), workCenterId: null)).Should().Be(5.0m);
        // An inactive rate is never used.
        (await resolver.ResolveAsync(CostingRateType.MachineOH, new DateOnly(2026, 7, 8))).Should().Be(0m);
    }

    // ── 2. Absorption true-up (M6) ──

    private static JournalPostingService Posting(ApplicationDbContext ctx) =>
        new(new Repository<JournalEntry, long>(ctx), new Repository<Account>(ctx),
            TestHarness.Numbering().Object, new StubCurrentUser(), new StubClock(),
            new PeriodGuard(ctx, new StubCurrentUser()));

    private PostAbsorptionTrueUpCommandHandler TrueUpHandler(ApplicationDbContext ctx) =>
        new(new Repository<JournalEntryLine, long>(ctx), new Repository<JournalEntry, long>(ctx),
            new Repository<Account>(ctx), new PeriodGuard(ctx, new StubCurrentUser()),
            TestHarness.Numbering().Object, new StubCurrentUser(), new UnitOfWork(ctx));

    private static decimal Bal(ApplicationDbContext ctx, int accId) =>
        ctx.JournalEntryLines.Where(l => l.AccountId == accId).Sum(l => l.Debit)
      - ctx.JournalEntryLines.Where(l => l.AccountId == accId).Sum(l => l.Credit);

    [Fact]
    public async Task True_up_relieves_applied_contra_to_variance_account()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoa(ctx);
        // Absorbed during the month: Cr 5211 300 (labour), Cr 5311 200 (FOH) — WIP debited.
        ctx.JournalEntries.Add(Posted("JV1", new DateOnly(2026, 3, 5), (1, 300m, 0m), (5, 0m, 300m)));
        ctx.JournalEntries.Add(Posted("JV2", new DateOnly(2026, 3, 6), (1, 200m, 0m), (7, 0m, 200m)));
        ctx.SaveChanges();

        var res = await TrueUpHandler(ctx).Handle(
            new PostAbsorptionTrueUpCommand(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31), new DateOnly(2026, 3, 31)), default);

        res.Success.Should().BeTrue();
        Bal(ctx, 5).Should().Be(0m);      // 5211 Applied Labour zeroed
        Bal(ctx, 7).Should().Be(0m);      // 5311 Applied FOH zeroed
        Bal(ctx, 3).Should().Be(-500m);   // 5165 credited the relieved 500 (reduces expense by absorbed amount)
    }

    [Fact]
    public async Task True_up_preview_shows_applied_vs_actual_variance()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoa(ctx);
        ctx.JournalEntries.Add(Posted("JV1", new DateOnly(2026, 3, 5), (1, 300m, 0m), (5, 0m, 300m)));   // applied labour 300
        ctx.JournalEntries.Add(Posted("PAY", new DateOnly(2026, 3, 7), (4, 500m, 0m), (1, 0m, 500m)));   // actual salary 500
        ctx.SaveChanges();

        var res = await new GetAbsorptionTrueUpPreviewQueryHandler(new Repository<JournalEntryLine, long>(ctx))
            .Handle(new GetAbsorptionTrueUpPreviewQuery(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31)), default);

        res.Data!.AppliedLabour.Should().Be(300m);
        res.Data.ActualLabour.Should().Be(500m);
        res.Data.Variance.Should().Be(200m);          // actual − applied
        res.Data.VarianceKind.Should().Be("Under-absorbed");
    }

    // ── 3. WIP snapshot (M7) ──

    [Fact]
    public async Task Wip_snapshot_posts_and_auto_reverses_next_day()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoa(ctx);
        ctx.Products.Add(new Product { Id = 20, Code = "FG", Name = "Label", WeightedAverageCost = 10m });
        // In-progress order: 50 units × WAC 10 → estimated WIP 500.
        ctx.ProductionOrders.Add(new ProductionOrder
        {
            Code = "PO-1", ProductId = 20, BomId = 1, Quantity = 50m,
            Status = ProductionOrderStatus.InProgress, IssueWarehouseId = 1, ReceiveWarehouseId = 1
        });
        ctx.SaveChanges();

        var handler = new PostWipSnapshotCommandHandler(
            new Repository<ProductionOrder, long>(ctx), new Repository<JournalEntry, long>(ctx),
            new Repository<Account>(ctx), new PeriodGuard(ctx, new StubCurrentUser()),
            TestHarness.Numbering().Object, new StubCurrentUser(), new UnitOfWork(ctx));

        var res = await handler.Handle(new PostWipSnapshotCommand(new DateOnly(2026, 3, 31)), default);

        res.Success.Should().BeTrue();
        var snap = ctx.JournalEntries.Single(j => j.SourceType == "WipSnapshot");
        snap.EntryDate.Should().Be(new DateOnly(2026, 3, 31));
        snap.Lines.Single(l => l.AccountId == 1).Debit.Should().Be(500m);   // Dr WIP
        var rev = ctx.JournalEntries.Single(j => j.SourceType == "WipSnapshotReversal");
        rev.EntryDate.Should().Be(new DateOnly(2026, 4, 1));                 // reverses next day
        rev.Lines.Single(l => l.AccountId == 1).Credit.Should().Be(500m);   // Cr WIP
        Bal(ctx, 1).Should().Be(0m);   // net across snapshot + reversal = 0 (WIP nets out over the two days)
    }
}
