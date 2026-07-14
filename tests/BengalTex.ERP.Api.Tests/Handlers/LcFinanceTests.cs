using BengalTex.ERP.Api.Tests.TestSupport;
using BengalTex.ERP.Application.Banking.Commands;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Infrastructure.Persistence;
using BengalTex.ERP.Infrastructure.Persistence.Repositories;
using BengalTex.ERP.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace BengalTex.ERP.Api.Tests.Handlers;

/// <summary>
/// Phase A6a — import LC financial events. Each event posts its own journal, turning the LC into a
/// bank-finance sub-ledger: margin locked (1185), document retirement into PAD (2180) / acceptance
/// (2190) with margin applied, interest (5860), and settlement.
/// </summary>
public class LcFinanceTests
{
    private static void SeedCoa(ApplicationDbContext ctx)
    {
        ctx.Accounts.AddRange(
            new Account { Id = 1, Code = "1110", Name = "Cash", AccountType = AccountType.Asset },
            new Account { Id = 2, Code = "1120", Name = "Bank", AccountType = AccountType.Asset },
            new Account { Id = 3, Code = "1185", Name = "LC Margin", AccountType = AccountType.Asset },
            new Account { Id = 4, Code = "2110", Name = "AP", AccountType = AccountType.Liability },
            new Account { Id = 5, Code = "2180", Name = "PAD", AccountType = AccountType.Liability },
            new Account { Id = 6, Code = "2190", Name = "Acceptance", AccountType = AccountType.Liability },
            new Account { Id = 7, Code = "5600", Name = "Bank Charges", AccountType = AccountType.Expense },
            new Account { Id = 8, Code = "5860", Name = "Interest", AccountType = AccountType.Expense });
        ctx.SaveChanges();
    }

    private static JournalPostingService Posting(ApplicationDbContext ctx) =>
        new(new Repository<JournalEntry, long>(ctx), new Repository<Account>(ctx),
            TestHarness.Numbering().Object, new StubCurrentUser(), new StubClock(),
            new PeriodGuard(ctx, new StubCurrentUser()));

    private static decimal Bal(ApplicationDbContext ctx, string code)
    {
        var accId = ctx.Accounts.Single(a => a.Code == code).Id;
        return ctx.JournalEntryLines.Where(l => l.AccountId == accId).Sum(l => l.Debit - l.Credit);
    }

    private static long SeedOpenLc(ApplicationDbContext ctx)
    {
        var lc = new LetterOfCredit
        {
            Code = "LC-1", LcNumber = "BANK-123", IssuingBank = "ABC Bank",
            SupplierId = 1, CurrencyId = 1, ExchangeRate = 1m, Amount = 100_000m,
            IssueDate = new DateOnly(2026, 6, 1), ExpiryDate = new DateOnly(2026, 9, 1), TenorDays = 90,
            Status = LcStatus.Open
        };
        ctx.LettersOfCredit.Add(lc);
        ctx.SaveChanges();
        return lc.Id;
    }

    private static AddLcFinancialEventCommandHandler Handler(ApplicationDbContext ctx) =>
        new(new Repository<LcFinancialEvent, long>(ctx), new Repository<LetterOfCredit, long>(ctx),
            new UnitOfWork(ctx), Posting(ctx));

    private static AddLcFinancialEventCommand Ev(long lcId, string type, decimal amount, decimal margin = 0m) =>
        new(lcId, type, new DateOnly(2026, 6, 15), amount, margin, "BankTransfer", null, null);

    [Fact]
    public async Task Margin_deposit_locks_cash_into_lc_margin()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoa(ctx);
        var lcId = SeedOpenLc(ctx);

        var res = await Handler(ctx).Handle(Ev(lcId, "MarginDeposit", 10_000m), default);

        res.Success.Should().BeTrue();
        Bal(ctx, "1185").Should().Be(10_000m);   // margin asset
        Bal(ctx, "1120").Should().Be(-10_000m);  // out of bank
    }

    [Fact]
    public async Task Retirement_and_settlement_move_payable_through_pad_and_close_the_lc()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoa(ctx);
        var lcId = SeedOpenLc(ctx);

        // Margin deposited, then documents retired at sight (bank finances the rest as PAD).
        await Handler(ctx).Handle(Ev(lcId, "MarginDeposit", 10_000m), default);
        await Handler(ctx).Handle(Ev(lcId, "RetirementSight", 100_000m, margin: 10_000m), default);

        Bal(ctx, "2110").Should().Be(100_000m);   // supplier payable cleared (Dr)
        Bal(ctx, "1185").Should().Be(0m);         // margin fully applied (10k dep − 10k applied)
        Bal(ctx, "2180").Should().Be(-90_000m);   // PAD liability to the bank
        ctx.LettersOfCredit.Single().Status.Should().Be(LcStatus.Shipped);

        // Interest (cost of credit) then settle the PAD — the LC auto-settles when nothing is outstanding.
        await Handler(ctx).Handle(Ev(lcId, "Interest", 1_500m), default);
        await Handler(ctx).Handle(Ev(lcId, "PadSettlement", 90_000m), default);

        Bal(ctx, "5860").Should().Be(1_500m);     // interest expensed
        Bal(ctx, "2180").Should().Be(0m);         // PAD cleared
        ctx.LettersOfCredit.Single().Status.Should().Be(LcStatus.Settled);
    }

    [Fact]
    public async Task Events_are_rejected_on_a_draft_lc_and_over_applied_margin()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoa(ctx);
        var lcId = SeedOpenLc(ctx);

        // Margin applied cannot exceed the retirement amount.
        (await Handler(ctx).Handle(Ev(lcId, "RetirementSight", 1_000m, margin: 2_000m), default))
            .Success.Should().BeFalse();

        // A Draft LC can't take events.
        var draft = new LetterOfCredit
        {
            Code = "LC-2", LcNumber = "X", IssuingBank = "Y", SupplierId = 1, CurrencyId = 1,
            ExchangeRate = 1m, Amount = 5_000m, IssueDate = new DateOnly(2026, 6, 1),
            ExpiryDate = new DateOnly(2026, 9, 1), TenorDays = 30, Status = LcStatus.Draft
        };
        ctx.LettersOfCredit.Add(draft);
        ctx.SaveChanges();
        (await Handler(ctx).Handle(Ev(draft.Id, "MarginDeposit", 500m), default))
            .Success.Should().BeFalse();
    }

    [Fact]
    public async Task Summary_reflects_running_bank_finance_balances()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoa(ctx);
        var lcId = SeedOpenLc(ctx);
        await Handler(ctx).Handle(Ev(lcId, "MarginDeposit", 10_000m), default);
        await Handler(ctx).Handle(Ev(lcId, "BankCharge", 800m), default);
        await Handler(ctx).Handle(Ev(lcId, "AcceptanceUsance", 100_000m, margin: 10_000m), default);

        var q = await new GetLcFinancialEventsQueryHandler(new Repository<LcFinancialEvent, long>(ctx))
            .Handle(new GetLcFinancialEventsQuery(lcId), default);

        q.Data!.Summary.MarginBalance.Should().Be(0m);
        q.Data.Summary.AcceptanceOutstanding.Should().Be(90_000m);
        q.Data.Summary.TotalCharges.Should().Be(800m);
        q.Data.Events.Should().HaveCount(3);
    }
}
