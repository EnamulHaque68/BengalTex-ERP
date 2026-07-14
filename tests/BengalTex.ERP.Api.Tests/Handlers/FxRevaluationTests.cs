using BengalTex.ERP.Api.Tests.TestSupport;
using BengalTex.ERP.Application.Accounting.Revaluation;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Infrastructure.Persistence;
using BengalTex.ERP.Infrastructure.Persistence.Repositories;
using BengalTex.ERP.Infrastructure.Services;
using FluentAssertions;
using Xunit;

namespace BengalTex.ERP.Api.Tests.Handlers;

/// <summary>
/// Phase A7b (C9) — month-end FC revaluation. Restates open foreign-currency AR/AP at the as-of
/// dated rate to Unrealized Exchange Gain (4310) / Loss (5810), auto-reversing the next day.
/// </summary>
public class FxRevaluationTests
{
    private static void Seed(ApplicationDbContext ctx)
    {
        ctx.Accounts.AddRange(
            new Account { Id = 1, Code = "1130", Name = "AR", AccountType = AccountType.Asset },
            new Account { Id = 2, Code = "2110", Name = "AP", AccountType = AccountType.Liability },
            new Account { Id = 3, Code = "4310", Name = "Unrealized FX Gain", AccountType = AccountType.Income },
            new Account { Id = 4, Code = "5810", Name = "Unrealized FX Loss", AccountType = AccountType.Expense });
        ctx.Currencies.Add(new Currency { Id = 2, Code = "USD", Name = "US Dollar", Symbol = "$", ExchangeRateToBase = 120m, IsBaseCurrency = false });
        // Month-end rate 125 (booked at 120 → +5 BDT per USD).
        ctx.ExchangeRates.Add(new ExchangeRate { CurrencyId = 2, RateDate = new DateOnly(2026, 6, 30), Rate = 125m });
        ctx.CustomerInvoices.Add(new CustomerInvoice { Code = "INV-1", CustomerId = 1, CurrencyId = 2, ExchangeRate = 120m, TotalAmount = 1_000m, AmountPaid = 0m, Status = CustomerInvoiceStatus.Issued, InvoiceDate = new DateOnly(2026, 6, 15) });
        ctx.SupplierInvoices.Add(new SupplierInvoice { Code = "SINV-1", SupplierId = 1, CurrencyId = 2, ExchangeRate = 120m, TotalAmount = 500m, AmountPaid = 0m, Status = SupplierInvoiceStatus.Approved, InvoiceDate = new DateOnly(2026, 6, 10) });
        ctx.SaveChanges();
    }

    private static decimal Bal(ApplicationDbContext ctx, string code)
    {
        var accId = ctx.Accounts.Single(a => a.Code == code).Id;
        return ctx.JournalEntryLines.Where(l => l.AccountId == accId).Sum(l => l.Debit - l.Credit);
    }

    private static PostFxRevaluationCommandHandler PostHandler(ApplicationDbContext ctx) =>
        new(new Repository<CustomerInvoice, long>(ctx), new Repository<SupplierInvoice, long>(ctx),
            new ExchangeRateResolver(ctx), new Repository<JournalEntry, long>(ctx), new Repository<Account>(ctx),
            new PeriodGuard(ctx, new StubCurrentUser()), TestHarness.Numbering().Object, new StubCurrentUser(), new UnitOfWork(ctx));

    [Fact]
    public async Task Preview_computes_unrealized_gain_on_ar_and_loss_on_ap()
    {
        await using var ctx = TestHarness.NewContext();
        Seed(ctx);

        var res = await new GetFxRevaluationPreviewQueryHandler(
            new Repository<CustomerInvoice, long>(ctx), new Repository<SupplierInvoice, long>(ctx), new ExchangeRateResolver(ctx))
            .Handle(new GetFxRevaluationPreviewQuery(new DateOnly(2026, 6, 30)), default);

        res.Success.Should().BeTrue();
        res.Data!.ArDelta.Should().Be(5_000m);   // 1000 USD × (125 − 120)
        res.Data.ApDelta.Should().Be(2_500m);    // 500 USD × (125 − 120)
        res.Data.NetUnrealized.Should().Be(2_500m);
        res.Data.Rows.Should().HaveCount(2);
    }

    [Fact]
    public async Task Posting_books_unrealized_gain_loss_and_auto_reverses_next_day()
    {
        await using var ctx = TestHarness.NewContext();
        Seed(ctx);

        var res = await PostHandler(ctx).Handle(new PostFxRevaluationCommand(new DateOnly(2026, 6, 30)), default);
        res.Success.Should().BeTrue();

        // Snapshot leg amounts on the as-of date.
        var snap = ctx.JournalEntries.Single(j => j.SourceType == "FxRevaluation");
        snap.EntryDate.Should().Be(new DateOnly(2026, 6, 30));
        snap.Lines.Single(l => l.AccountId == 1).Debit.Should().Be(5_000m);   // Dr AR (worth more)
        snap.Lines.Single(l => l.AccountId == 3).Credit.Should().Be(5_000m);  // Cr 4310 gain
        snap.Lines.Single(l => l.AccountId == 4).Debit.Should().Be(2_500m);   // Dr 5810 loss (AP worth more)
        snap.Lines.Single(l => l.AccountId == 2).Credit.Should().Be(2_500m);  // Cr AP

        var rev = ctx.JournalEntries.Single(j => j.SourceType == "FxRevaluationReversal");
        rev.EntryDate.Should().Be(new DateOnly(2026, 7, 1));

        // Snapshot + reversal net every account to zero (no permanent AR/AP distortion).
        Bal(ctx, "1130").Should().Be(0m);
        Bal(ctx, "2110").Should().Be(0m);
        Bal(ctx, "4310").Should().Be(0m);
        Bal(ctx, "5810").Should().Be(0m);
    }

    [Fact]
    public async Task Nothing_to_revalue_when_no_open_fc_balances()
    {
        await using var ctx = TestHarness.NewContext();
        ctx.Accounts.AddRange(
            new Account { Id = 1, Code = "1130", Name = "AR", AccountType = AccountType.Asset },
            new Account { Id = 2, Code = "2110", Name = "AP", AccountType = AccountType.Liability },
            new Account { Id = 3, Code = "4310", Name = "Gain", AccountType = AccountType.Income },
            new Account { Id = 4, Code = "5810", Name = "Loss", AccountType = AccountType.Expense });
        ctx.SaveChanges();

        (await PostHandler(ctx).Handle(new PostFxRevaluationCommand(new DateOnly(2026, 6, 30)), default))
            .Success.Should().BeFalse();
    }
}
