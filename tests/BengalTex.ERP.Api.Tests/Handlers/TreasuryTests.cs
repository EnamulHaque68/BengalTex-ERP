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
/// Phase A6c — Treasury (bank facilities: loan / OD / FDR) + dated exchange rates. Facility events
/// post journals (Dr Bank / Cr 2210 on drawdown, Dr 5860 / Cr Bank interest, Dr 1250 / Cr Bank FDR,
/// …) and the rate resolver returns the as-of dated rate or the currency's current rate.
/// </summary>
public class TreasuryTests
{
    private static void SeedCoa(ApplicationDbContext ctx)
    {
        ctx.Accounts.AddRange(
            new Account { Id = 1, Code = "1110", Name = "Cash", AccountType = AccountType.Asset },
            new Account { Id = 2, Code = "1120", Name = "Bank", AccountType = AccountType.Asset },
            new Account { Id = 3, Code = "1250", Name = "FDR", AccountType = AccountType.Asset },
            new Account { Id = 4, Code = "2210", Name = "Bank Loan", AccountType = AccountType.Liability },
            new Account { Id = 5, Code = "4200", Name = "Other Income", AccountType = AccountType.Income },
            new Account { Id = 6, Code = "5860", Name = "Interest", AccountType = AccountType.Expense });
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

    private static AddBankFacilityEventCommandHandler EventHandler(ApplicationDbContext ctx) =>
        new(new Repository<BankFacilityEvent, long>(ctx), new Repository<BankFacility, long>(ctx),
            new UnitOfWork(ctx), Posting(ctx));

    private static async Task<long> CreateFacility(ApplicationDbContext ctx, string type)
    {
        var res = await new CreateBankFacilityCommandHandler(
            new Repository<BankFacility, long>(ctx), new UnitOfWork(ctx), TestHarness.Numbering().Object)
            .Handle(new CreateBankFacilityCommand(type, "ABC Bank", "AC-1", 1_000_000m, 9m, new DateOnly(2026, 6, 1), null, null), default);
        return ctx.BankFacilities.Single(f => f.Id == res.Data).Id;
    }

    private static AddBankFacilityEventCommand Ev(long id, string type, decimal amount) =>
        new(id, type, new DateOnly(2026, 6, 15), amount, "BankTransfer", null, null);

    // ── T1 — treasury facilities ──

    [Fact]
    public async Task Loan_drawdown_interest_and_repayment_post_and_close_the_facility()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoa(ctx);
        var id = await CreateFacility(ctx, "TermLoan");

        await EventHandler(ctx).Handle(Ev(id, "Drawdown", 1_000_000m), default);
        await EventHandler(ctx).Handle(Ev(id, "InterestCharge", 8_000m), default);

        Bal(ctx, "1120").Should().Be(992_000m);    // +1,000,000 drawn − 8,000 interest paid
        Bal(ctx, "2210").Should().Be(-1_000_000m); // loan liability
        Bal(ctx, "5860").Should().Be(8_000m);      // interest expensed

        await EventHandler(ctx).Handle(Ev(id, "PrincipalRepayment", 1_000_000m), default);
        Bal(ctx, "2210").Should().Be(0m);          // loan cleared
        ctx.BankFacilities.Single().Status.Should().Be(BankFacilityStatus.Closed);
    }

    [Fact]
    public async Task Fdr_placement_and_interest_income_post_to_the_right_accounts()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoa(ctx);
        var id = await CreateFacility(ctx, "Fdr");

        await EventHandler(ctx).Handle(Ev(id, "FdrPlacement", 500_000m), default);
        await EventHandler(ctx).Handle(Ev(id, "FdrInterestIncome", 25_000m), default);

        Bal(ctx, "1250").Should().Be(500_000m);   // FDR asset
        Bal(ctx, "4200").Should().Be(-25_000m);   // interest income
        Bal(ctx, "1120").Should().Be(-475_000m);  // −500k placed + 25k income
    }

    [Fact]
    public async Task Fdr_event_on_a_loan_facility_is_rejected()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoa(ctx);
        var id = await CreateFacility(ctx, "TermLoan");

        (await EventHandler(ctx).Handle(Ev(id, "FdrPlacement", 100_000m), default))
            .Success.Should().BeFalse();
    }

    // ── T3 — dated exchange rates ──

    [Fact]
    public async Task Resolver_returns_the_as_of_dated_rate_else_the_currency_rate()
    {
        await using var ctx = TestHarness.NewContext();
        ctx.Currencies.Add(new Currency { Id = 2, Code = "USD", Name = "US Dollar", Symbol = "$", ExchangeRateToBase = 120m });
        ctx.ExchangeRates.AddRange(
            new ExchangeRate { CurrencyId = 2, RateDate = new DateOnly(2026, 6, 1), Rate = 118m },
            new ExchangeRate { CurrencyId = 2, RateDate = new DateOnly(2026, 6, 15), Rate = 122m });
        ctx.SaveChanges();
        var resolver = new ExchangeRateResolver(ctx);

        (await resolver.GetRateAsOfAsync(2, new DateOnly(2026, 6, 20))).Should().Be(122m);  // latest ≤ date
        (await resolver.GetRateAsOfAsync(2, new DateOnly(2026, 6, 10))).Should().Be(118m);  // earlier dated
        (await resolver.GetRateAsOfAsync(2, new DateOnly(2026, 5, 1))).Should().Be(120m);   // none on file → current
    }
}
