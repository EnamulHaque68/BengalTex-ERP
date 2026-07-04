using BengalTex.ERP.Api.Tests.TestSupport;
using BengalTex.ERP.Application.Accounting.Commands;
using BengalTex.ERP.Application.Accounting.Dtos;
using BengalTex.ERP.Application.Accounting.Fiscal;
using BengalTex.ERP.Application.Accounting.Queries;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Infrastructure.Persistence;
using BengalTex.ERP.Infrastructure.Persistence.Repositories;
using BengalTex.ERP.Infrastructure.Services;
using BengalTex.ERP.Shared.Common;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BengalTex.ERP.Api.Tests.Handlers;

/// <summary>
/// Phase A1 — Fiscal Rails &amp; Voucher Architecture. Covers the period-guard matrix, the
/// contra voucher, opening-balance import, year-end close/reopen, reversal linkage and the
/// JV approval gate.
/// </summary>
public class FiscalRailsTests
{
    // ── shared plumbing ──────────────────────────────────────────────────────

    /// <summary>Current-user stub with a switchable Close-Books permission.</summary>
    private sealed class PermUser : ICurrentUserService
    {
        public bool CloseBooksAllowed { get; set; }
        public string? UserId => "t";
        public string? UserName => "Tester";
        public int? FactoryId => null;
        public string? IpAddress => null;
        public string? UserAgent => null;
        public bool IsAuthenticated => true;
        public IReadOnlyList<string> Roles => new[] { "Admin" };
        public bool IsInRole(string role) => true;
        public bool HasPermission(string permission) =>
            permission != BengalTex.ERP.Shared.Permissions.Permissions.Accounting.CloseBooks || CloseBooksAllowed;
        public IReadOnlyList<string> Permissions => Array.Empty<string>();
    }

    private static readonly Mock<IMediator> MediatorStub = BuildMediatorStub();
    private static Mock<IMediator> BuildMediatorStub()
    {
        var m = new Mock<IMediator>();
        m.Setup(x => x.Send(It.IsAny<GetJournalEntryByIdQuery>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(ApiResponse<JournalEntryDto>.Ok(null!));
        return m;
    }

    /// <summary>Seeds one FY (Jan–Dec 2026) and returns its periods keyed by month.</summary>
    private static async Task<FinancialYear> SeedYear(
        ApplicationDbContext ctx, AccountingPeriodStatus janStatus = AccountingPeriodStatus.Open)
    {
        var fy = new FinancialYear
        {
            Code = "FY2026", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 12, 31),
            Status = FinancialYearStatus.Open
        };
        for (var i = 0; i < 12; i++)
        {
            var s = fy.StartDate.AddMonths(i);
            fy.Periods.Add(new AccountingPeriod
            {
                PeriodNumber = i + 1, Name = s.ToString("MMM yyyy"),
                StartDate = s, EndDate = s.AddMonths(1).AddDays(-1),
                Status = i == 0 ? janStatus : AccountingPeriodStatus.Open
            });
        }
        ctx.FinancialYears.Add(fy);
        await ctx.SaveChangesAsync();
        return fy;
    }

    private static void SeedCoreAccounts(ApplicationDbContext ctx)
    {
        ctx.Accounts.AddRange(
            new Account { Id = 1, Code = "1110", Name = "Cash", AccountType = AccountType.Asset },
            new Account { Id = 2, Code = "1120", Name = "Bank", AccountType = AccountType.Asset },
            new Account { Id = 3, Code = "1130", Name = "AR", AccountType = AccountType.Asset },
            new Account { Id = 4, Code = "3150", Name = "Opening Equity", AccountType = AccountType.Equity },
            new Account { Id = 5, Code = "3200", Name = "Retained Earnings", AccountType = AccountType.Equity },
            new Account { Id = 6, Code = "4100", Name = "Sales", AccountType = AccountType.Income },
            new Account { Id = 7, Code = "5100", Name = "COGS", AccountType = AccountType.Expense });
    }

    private static JournalEntry PostedEntry(string code, DateOnly date, int drAccount, int crAccount, decimal amount) => new()
    {
        Code = code, EntryDate = date, Status = JournalEntryStatus.Posted,
        PostedAt = DateTimeOffset.UtcNow, PostedBy = "seed",
        Lines =
        {
            new JournalEntryLine { AccountId = drAccount, Debit = amount, Credit = 0m, SortOrder = 0 },
            new JournalEntryLine { AccountId = crAccount, Debit = 0m, Credit = amount, SortOrder = 1 }
        }
    };

    // ── 1. Period-guard matrix ───────────────────────────────────────────────

    [Fact]
    public async Task Guard_allows_everything_when_no_fiscal_year_exists()
    {
        await using var ctx = TestHarness.NewContext();
        var guard = new PeriodGuard(ctx, new PermUser());

        (await guard.CheckAsync(new DateOnly(2026, 1, 15), isManualVoucher: true)).Should().BeNull();
        (await guard.CheckAsync(new DateOnly(2026, 1, 15), isManualVoucher: false)).Should().BeNull();
        (await guard.GetPeriodIdAsync(new DateOnly(2026, 1, 15))).Should().BeNull();
    }

    [Fact]
    public async Task Guard_matrix_open_softclosed_locked()
    {
        await using var ctx = TestHarness.NewContext();
        var fy = await SeedYear(ctx, AccountingPeriodStatus.SoftClosed);
        var user = new PermUser();
        var guard = new PeriodGuard(ctx, user);
        var jan15 = new DateOnly(2026, 1, 15);
        var feb15 = new DateOnly(2026, 2, 15);

        // Open period → everything allowed.
        (await guard.CheckAsync(feb15, true)).Should().BeNull();

        // SoftClosed: auto-journals pass, manual blocked without CloseBooks, allowed with it.
        (await guard.CheckAsync(jan15, isManualVoucher: false)).Should().BeNull();
        (await guard.CheckAsync(jan15, isManualVoucher: true)).Should().Contain("soft-closed");
        user.CloseBooksAllowed = true;
        (await guard.CheckAsync(jan15, isManualVoucher: true)).Should().BeNull();

        // Locked: blocked for all paths.
        var jan = fy.Periods.First(p => p.PeriodNumber == 1);
        jan.Status = AccountingPeriodStatus.Locked;
        await ctx.SaveChangesAsync();
        (await guard.CheckAsync(jan15, isManualVoucher: false)).Should().Contain("locked");
        (await guard.CheckAsync(jan15, isManualVoucher: true)).Should().Contain("locked");
        (await guard.GetPeriodIdAsync(jan15)).Should().Be(jan.Id);
    }

    [Fact]
    public async Task Posting_engine_refuses_a_locked_period()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoreAccounts(ctx);
        await SeedYear(ctx, AccountingPeriodStatus.Locked);

        var service = new JournalPostingService(
            new Repository<JournalEntry, long>(ctx), new Repository<Account>(ctx),
            TestHarness.Numbering().Object, new PermUser(), new StubClock(),
            new PeriodGuard(ctx, new PermUser()));

        var act = () => service.PostAsync(
            new DateOnly(2026, 1, 10), "test", "CustomerInvoice", 1, "INV-1",
            new[] { new JournalPostingLine("1130", 100m, 0m), new JournalPostingLine("4100", 0m, 100m) });

        await act.Should().ThrowAsync<FluentValidation.ValidationException>();
        ctx.JournalEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task Posting_engine_classifies_voucher_type_and_stamps_period()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoreAccounts(ctx);
        var fy = await SeedYear(ctx);

        var service = new JournalPostingService(
            new Repository<JournalEntry, long>(ctx), new Repository<Account>(ctx),
            TestHarness.Numbering().Object, new PermUser(), new StubClock(),
            new PeriodGuard(ctx, new PermUser()));

        await service.PostAsync(new DateOnly(2026, 3, 5), "receipt", "Receipt", 1, "RCT-1",
            new[] { new JournalPostingLine("1110", 100m, 0m), new JournalPostingLine("1130", 0m, 100m) });
        await ctx.SaveChangesAsync();

        var je = ctx.JournalEntries.Single();
        je.VoucherType.Should().Be(VoucherType.Receipt);
        je.Code.Should().StartWith("RV");   // receipt series
        je.AccountingPeriodId.Should().Be(fy.Periods.First(p => p.PeriodNumber == 3).Id);
    }

    // ── 2. Contra voucher ────────────────────────────────────────────────────

    private CreateContraVoucherCommandHandler ContraHandler(ApplicationDbContext ctx) =>
        new(new Repository<JournalEntry, long>(ctx), new Repository<Account>(ctx),
            new PeriodGuard(ctx, new PermUser()), TestHarness.Numbering().Object,
            new PermUser(), new UnitOfWork(ctx), MediatorStub.Object);

    [Fact]
    public async Task Contra_moves_funds_between_cash_and_bank()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoreAccounts(ctx);
        await ctx.SaveChangesAsync();

        var res = await ContraHandler(ctx).Handle(new CreateContraVoucherCommand(
            new DateOnly(2026, 1, 10), FromAccountId: 2, ToAccountId: 1, 5000m, "CHQ-1", null), default);

        res.Success.Should().BeTrue();
        var je = ctx.JournalEntries.Single();
        je.VoucherType.Should().Be(VoucherType.Contra);
        je.Status.Should().Be(JournalEntryStatus.Posted);
        je.Lines.Single(l => l.Debit > 0).AccountId.Should().Be(1);    // Dr Cash (destination)
        je.Lines.Single(l => l.Credit > 0).AccountId.Should().Be(2);   // Cr Bank (source)
    }

    [Fact]
    public async Task Contra_rejects_a_non_cash_bank_account()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoreAccounts(ctx);
        await ctx.SaveChangesAsync();

        var res = await ContraHandler(ctx).Handle(new CreateContraVoucherCommand(
            new DateOnly(2026, 1, 10), FromAccountId: 3 /* AR */, ToAccountId: 1, 5000m, null, null), default);

        res.Success.Should().BeFalse();
        res.Message.Should().Contain("cash/bank");
        ctx.JournalEntries.Should().BeEmpty();
    }

    // ── 3. Opening balances ──────────────────────────────────────────────────

    private ImportOpeningBalancesCommandHandler OpeningHandler(ApplicationDbContext ctx) =>
        new(new Repository<JournalEntry, long>(ctx), new Repository<Account>(ctx),
            new PeriodGuard(ctx, new PermUser { CloseBooksAllowed = true }),
            TestHarness.Numbering().Object, new PermUser(), new UnitOfWork(ctx));

    [Fact]
    public async Task Opening_import_plugs_the_imbalance_to_equity()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoreAccounts(ctx);
        await ctx.SaveChangesAsync();

        var res = await OpeningHandler(ctx).Handle(new ImportOpeningBalancesCommand(
            new DateOnly(2026, 1, 1),
            new[] { new OpeningBalanceLineInput(1, 70000m, 0m), new OpeningBalanceLineInput(3, 30000m, 0m) }), default);

        res.Success.Should().BeTrue();
        var je = ctx.JournalEntries.Single();
        je.VoucherType.Should().Be(VoucherType.Opening);
        je.Lines.Should().HaveCount(3);
        var plug = je.Lines.Single(l => l.AccountId == 4);   // 3150
        plug.Credit.Should().Be(100000m);                    // balances the two debits
        je.Lines.Sum(l => l.Debit).Should().Be(je.Lines.Sum(l => l.Credit));
    }

    [Fact]
    public async Task Second_opening_import_is_blocked_until_reversed()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoreAccounts(ctx);
        await ctx.SaveChangesAsync();
        var handler = OpeningHandler(ctx);
        (await handler.Handle(new ImportOpeningBalancesCommand(
            new DateOnly(2026, 1, 1), new[] { new OpeningBalanceLineInput(1, 100m, 0m) }), default))
            .Success.Should().BeTrue();

        var second = await handler.Handle(new ImportOpeningBalancesCommand(
            new DateOnly(2026, 1, 1), new[] { new OpeningBalanceLineInput(1, 200m, 0m) }), default);

        second.Success.Should().BeFalse();
        second.Message.Should().Contain("already exists");
    }

    // ── 4. Year-end close / reopen ───────────────────────────────────────────

    [Fact]
    public async Task Year_close_requires_locked_periods_then_sweeps_pl_to_retained_earnings()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoreAccounts(ctx);
        var fy = await SeedYear(ctx);
        // Income 1000 (Cr 4100), expense 400 (Dr 5100) → net profit 600.
        ctx.JournalEntries.Add(PostedEntry("JV-1", new DateOnly(2026, 2, 1), 3, 6, 1000m));
        ctx.JournalEntries.Add(PostedEntry("JV-2", new DateOnly(2026, 3, 1), 7, 1, 400m));
        await ctx.SaveChangesAsync();

        var handler = new CloseFinancialYearCommandHandler(
            new Repository<FinancialYear>(ctx), new Repository<JournalEntry, long>(ctx),
            new Repository<JournalEntryLine, long>(ctx), new Repository<Account>(ctx),
            new PeriodGuard(ctx, new PermUser()), TestHarness.Numbering().Object,
            new PermUser(), new UnitOfWork(ctx));

        // Blocked while periods are open.
        (await handler.Handle(new CloseFinancialYearCommand(fy.Id), default)).Success.Should().BeFalse();

        foreach (var p in ctx.AccountingPeriods) p.Status = AccountingPeriodStatus.Locked;
        await ctx.SaveChangesAsync();

        var res = await handler.Handle(new CloseFinancialYearCommand(fy.Id), default);
        res.Success.Should().BeTrue();

        var closing = ctx.JournalEntries.Single(j => j.VoucherType == VoucherType.Closing);
        closing.Lines.Single(l => l.AccountId == 5).Credit.Should().Be(600m);   // RE takes the profit
        ctx.FinancialYears.Single().Status.Should().Be(FinancialYearStatus.Closed);

        // P&L for the year still shows the pre-close figures (Closing excluded).
        var pl = await new GetProfitAndLossQueryHandler(
                new Repository<JournalEntryLine, long>(ctx), new Repository<Account>(ctx))
            .Handle(new GetProfitAndLossQuery(fy.StartDate, fy.EndDate), default);
        pl.Data!.NetProfit.Should().Be(600m);
    }

    [Fact]
    public async Task Reopen_reverses_the_closing_voucher()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoreAccounts(ctx);
        var fy = await SeedYear(ctx);
        ctx.JournalEntries.Add(PostedEntry("JV-1", new DateOnly(2026, 2, 1), 3, 6, 500m));
        foreach (var p in ctx.AccountingPeriods.Local.Concat(ctx.AccountingPeriods)) p.Status = AccountingPeriodStatus.Locked;
        await ctx.SaveChangesAsync();

        await new CloseFinancialYearCommandHandler(
                new Repository<FinancialYear>(ctx), new Repository<JournalEntry, long>(ctx),
                new Repository<JournalEntryLine, long>(ctx), new Repository<Account>(ctx),
                new PeriodGuard(ctx, new PermUser()), TestHarness.Numbering().Object,
                new PermUser(), new UnitOfWork(ctx))
            .Handle(new CloseFinancialYearCommand(fy.Id), default);

        var res = await new ReopenFinancialYearCommandHandler(
                new Repository<FinancialYear>(ctx), new Repository<JournalEntry, long>(ctx),
                TestHarness.Numbering().Object, new PermUser(), new UnitOfWork(ctx))
            .Handle(new ReopenFinancialYearCommand(fy.Id, "Audit adjustment"), default);

        res.Success.Should().BeTrue();
        ctx.FinancialYears.Single().Status.Should().Be(FinancialYearStatus.Open);
        var closings = ctx.JournalEntries.Where(j => j.VoucherType == VoucherType.Closing).ToList();
        closings.Should().HaveCount(2);                                     // close + its reversal
        closings.Should().Contain(j => j.ReversedEntryId != null);          // linked
    }

    // ── 5. Reversal ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Reversal_mirrors_and_blocks_double_reverse()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoreAccounts(ctx);
        ctx.JournalEntries.Add(PostedEntry("JV-9", new DateOnly(2026, 4, 1), 1, 6, 250m));
        await ctx.SaveChangesAsync();
        var originalId = ctx.JournalEntries.Single().Id;

        var handler = new ReverseJournalEntryCommandHandler(
            new Repository<JournalEntry, long>(ctx), new PeriodGuard(ctx, new PermUser()),
            TestHarness.Numbering().Object, new PermUser(), new UnitOfWork(ctx), MediatorStub.Object);

        var res = await handler.Handle(new ReverseJournalEntryCommand(originalId, "wrong account"), default);
        res.Success.Should().BeTrue();

        var reversal = ctx.JournalEntries.Single(j => j.ReversedEntryId == originalId);
        reversal.Lines.Single(l => l.AccountId == 1).Credit.Should().Be(250m);   // mirrored
        reversal.ReversalReason.Should().Be("wrong account");

        var again = await handler.Handle(new ReverseJournalEntryCommand(originalId, "again"), default);
        again.Success.Should().BeFalse();
        again.Message.Should().Contain("already been reversed");
    }

    // ── 6. Approval gate ─────────────────────────────────────────────────────

    [Fact]
    public async Task Over_threshold_manual_jv_waits_for_approval_then_bypass_posts_it()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoreAccounts(ctx);
        var draft = new JournalEntry
        {
            Code = "JV-BIG", EntryDate = new DateOnly(2026, 5, 1), Status = JournalEntryStatus.Draft,
            Lines =
            {
                new JournalEntryLine { AccountId = 1, Debit = 500000m, Credit = 0m, SortOrder = 0 },
                new JournalEntryLine { AccountId = 6, Debit = 0m, Credit = 500000m, SortOrder = 1 }
            }
        };
        ctx.JournalEntries.Add(draft);
        await ctx.SaveChangesAsync();

        var approval = new ApprovalService(
            ctx, Options.Create(new ApprovalSettings { JournalEntryThreshold = 100000m }),
            new StubClock(), new PermUser(), new Mock<INotificationService>().Object);
        var handler = new PostJournalEntryCommandHandler(
            new Repository<JournalEntry, long>(ctx), new PeriodGuard(ctx, new PermUser()),
            approval, new UnitOfWork(ctx), new PermUser(), MediatorStub.Object);

        var res = await handler.Handle(new PostJournalEntryCommand(draft.Id), default);
        res.Success.Should().BeTrue();
        ctx.JournalEntries.Single().Status.Should().Be(JournalEntryStatus.PendingApproval);

        // Approval decision path posts with the bypass flag.
        var posted = await handler.Handle(new PostJournalEntryCommand(draft.Id, BypassApprovalGate: true), default);
        posted.Success.Should().BeTrue();
        ctx.JournalEntries.Single().Status.Should().Be(JournalEntryStatus.Posted);
    }
}
