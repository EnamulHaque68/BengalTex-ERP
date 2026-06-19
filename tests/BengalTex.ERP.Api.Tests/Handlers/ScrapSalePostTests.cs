using BengalTex.ERP.Api.Tests.TestSupport;
using BengalTex.ERP.Application.Accounting;
using BengalTex.ERP.Application.ScrapSales;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Infrastructure.Persistence;
using BengalTex.ERP.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Moq;
using Xunit;

namespace BengalTex.ERP.Api.Tests.Handlers;

/// <summary>
/// Scrap-sale posting is financial-only (scrap isn't carried as stock): it books one balanced
/// journal Dr Cash|Bank / Cr Scrap Sales Income for Σ(qty × unit price), picks Cash vs Bank by
/// payment method, and flips the document to Posted (immutable thereafter).
/// </summary>
public class ScrapSalePostTests
{
    private readonly Mock<IJournalPostingService> _journal = new();
    private readonly List<(string Account, decimal Debit, decimal Credit)[]> _journalCalls = new();

    public ScrapSalePostTests()
    {
        _journal.Setup(j => j.PostAsync(
                It.IsAny<DateOnly>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(),
                It.IsAny<string>(), It.IsAny<IReadOnlyList<JournalPostingLine>>(), It.IsAny<CancellationToken>()))
            .Callback<DateOnly, string, string, long, string, IReadOnlyList<JournalPostingLine>, CancellationToken>(
                (_, _, _, _, _, lines, _) =>
                    _journalCalls.Add(lines.Select(l => (l.AccountCode, l.Debit, l.Credit)).ToArray()))
            .Returns(Task.CompletedTask);
    }

    private PostScrapSaleCommandHandler Handler(ApplicationDbContext ctx) =>
        new(new Repository<ScrapSale, long>(ctx), _journal.Object, new StubCurrentUser(), new UnitOfWork(ctx));

    private static ScrapSale Sale(PaymentMethod method) => new()
    {
        Code = "SCRAP-1", SaleDate = new DateOnly(2026, 6, 19), PaymentMethod = method,
        Status = ScrapSaleStatus.Draft,
        Lines =
        {
            new ScrapSaleLine { Description = "Fabric trim", Quantity = 10m, UnitPrice = 5m, SortOrder = 0 },
            new ScrapSaleLine { Description = "Paper waste", Quantity = 4m, UnitPrice = 2.5m, SortOrder = 1 }
        }
    };  // total = 50 + 10 = 60

    [Fact]
    public async Task Cash_sale_debits_cash_and_credits_scrap_income_then_posts()
    {
        await using var ctx = TestHarness.NewContext();
        ctx.ScrapSales.Add(Sale(PaymentMethod.Cash));
        await ctx.SaveChangesAsync();

        var result = await Handler(ctx).Handle(new PostScrapSaleCommand(1), default);

        result.Success.Should().BeTrue();
        _journalCalls.Should().ContainSingle();
        var legs = _journalCalls[0];
        legs.Single(l => l.Account == LedgerAccounts.Cash).Debit.Should().Be(60m);
        legs.Single(l => l.Account == LedgerAccounts.ScrapSalesIncome).Credit.Should().Be(60m);
        legs.Should().NotContain(l => l.Account == LedgerAccounts.Bank);

        ctx.ScrapSales.Single().Status.Should().Be(ScrapSaleStatus.Posted);
    }

    [Fact]
    public async Task Bank_sale_debits_bank_not_cash()
    {
        await using var ctx = TestHarness.NewContext();
        ctx.ScrapSales.Add(Sale(PaymentMethod.BankTransfer));
        await ctx.SaveChangesAsync();

        var result = await Handler(ctx).Handle(new PostScrapSaleCommand(1), default);

        result.Success.Should().BeTrue();
        var legs = _journalCalls[0];
        legs.Single(l => l.Account == LedgerAccounts.Bank).Debit.Should().Be(60m);
        legs.Should().NotContain(l => l.Account == LedgerAccounts.Cash);
    }

    [Fact]
    public async Task Posting_an_already_posted_sale_fails()
    {
        await using var ctx = TestHarness.NewContext();
        var sale = Sale(PaymentMethod.Cash);
        sale.Status = ScrapSaleStatus.Posted;
        ctx.ScrapSales.Add(sale);
        await ctx.SaveChangesAsync();

        var result = await Handler(ctx).Handle(new PostScrapSaleCommand(1), default);

        result.Success.Should().BeFalse();
        _journalCalls.Should().BeEmpty();
    }
}
