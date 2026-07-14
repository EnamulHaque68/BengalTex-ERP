using BengalTex.ERP.Api.Tests.TestSupport;
using BengalTex.ERP.Application.Banking.Commands;
using BengalTex.ERP.Application.Receipt.Commands;
using BengalTex.ERP.Application.Receipt.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Infrastructure.Persistence;
using BengalTex.ERP.Infrastructure.Persistence.Repositories;
using BengalTex.ERP.Infrastructure.Services;
using BengalTex.ERP.Shared.Common;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace BengalTex.ERP.Api.Tests.Handlers;

/// <summary>
/// Phase A6b — export realization (FDBP) + cash incentive. A receipt can carry bank charge (5600)
/// and interest (5860) deducted from the export proceeds; the incentive is accrued (Dr 1186 / Cr
/// 4260) and cleared on receipt (Dr Bank / Cr 1186).
/// </summary>
public class ExportFinanceTests
{
    private static void SeedCoa(ApplicationDbContext ctx)
    {
        ctx.Accounts.AddRange(
            new Account { Id = 1, Code = "1110", Name = "Cash", AccountType = AccountType.Asset },
            new Account { Id = 2, Code = "1120", Name = "Bank", AccountType = AccountType.Asset },
            new Account { Id = 3, Code = "1130", Name = "AR", AccountType = AccountType.Asset },
            new Account { Id = 4, Code = "1186", Name = "Export Incentive Receivable", AccountType = AccountType.Asset },
            new Account { Id = 5, Code = "4260", Name = "Export Incentive Income", AccountType = AccountType.Income },
            new Account { Id = 6, Code = "4300", Name = "Exchange Gain", AccountType = AccountType.Income },
            new Account { Id = 7, Code = "5600", Name = "Bank Charges", AccountType = AccountType.Expense },
            new Account { Id = 8, Code = "5800", Name = "Exchange Loss", AccountType = AccountType.Expense },
            new Account { Id = 9, Code = "5860", Name = "Interest", AccountType = AccountType.Expense });
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

    private static Mock<IMediator> ReceiptMediator()
    {
        var m = new Mock<IMediator>();
        m.Setup(x => x.Send(It.IsAny<Application.Receipt.Queries.GetReceiptByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<ReceiptDto>.Ok(null!));
        return m;
    }

    // ── F1 — FDBP export realization ──

    [Fact]
    public async Task Fdbp_receipt_expenses_charge_and_interest_out_of_proceeds()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoa(ctx);
        var inv = new CustomerInvoice { Code = "INV-1", CustomerId = 1, CurrencyId = 1, ExchangeRate = 1m, TotalAmount = 100_000m, AmountPaid = 0m, Status = CustomerInvoiceStatus.Issued };
        ctx.CustomerInvoices.Add(inv);
        ctx.SaveChanges();
        var rct = new Receipt { Code = "RCT-1", CustomerInvoiceId = inv.Id, ReceiptDate = new DateOnly(2026, 6, 10), Amount = 100_000m, ExchangeRate = 1m, PaymentMethod = PaymentMethod.BankTransfer, Status = ReceiptStatus.Draft, BankChargeAmount = 2_000m, InterestAmount = 1_000m };
        ctx.Receipts.Add(rct);
        ctx.SaveChanges();

        var post = new PostReceiptCommandHandler(
            new Repository<Receipt, long>(ctx), new Repository<CustomerInvoice, long>(ctx),
            new UnitOfWork(ctx), new StubCurrentUser(), Posting(ctx), ReceiptMediator().Object);
        var res = await post.Handle(new PostReceiptCommand(rct.Id), default);

        res.Success.Should().BeTrue();
        Bal(ctx, "1120").Should().Be(97_000m);    // net proceeds to bank
        Bal(ctx, "1130").Should().Be(-100_000m);  // AR cleared in full
        Bal(ctx, "5600").Should().Be(2_000m);     // bank charge expensed
        Bal(ctx, "5860").Should().Be(1_000m);     // interest expensed
    }

    [Fact]
    public async Task Cancelling_an_fdbp_receipt_reverses_every_leg()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoa(ctx);
        var inv = new CustomerInvoice { Code = "INV-1", CustomerId = 1, CurrencyId = 1, ExchangeRate = 1m, TotalAmount = 100_000m, AmountPaid = 0m, Status = CustomerInvoiceStatus.Issued };
        ctx.CustomerInvoices.Add(inv);
        ctx.SaveChanges();
        var rct = new Receipt { Code = "RCT-1", CustomerInvoiceId = inv.Id, ReceiptDate = new DateOnly(2026, 6, 10), Amount = 100_000m, ExchangeRate = 1m, PaymentMethod = PaymentMethod.BankTransfer, Status = ReceiptStatus.Draft, BankChargeAmount = 2_000m, InterestAmount = 1_000m };
        ctx.Receipts.Add(rct);
        ctx.SaveChanges();

        await new PostReceiptCommandHandler(
            new Repository<Receipt, long>(ctx), new Repository<CustomerInvoice, long>(ctx),
            new UnitOfWork(ctx), new StubCurrentUser(), Posting(ctx), ReceiptMediator().Object)
            .Handle(new PostReceiptCommand(rct.Id), default);

        await new CancelReceiptCommandHandler(
            new Repository<Receipt, long>(ctx), new Repository<CustomerInvoice, long>(ctx),
            new UnitOfWork(ctx), Posting(ctx), ReceiptMediator().Object)
            .Handle(new CancelReceiptCommand(rct.Id), default);

        Bal(ctx, "1120").Should().Be(0m);
        Bal(ctx, "1130").Should().Be(0m);
        Bal(ctx, "5600").Should().Be(0m);
        Bal(ctx, "5860").Should().Be(0m);
    }

    // ── F2 — cash incentive ──

    private CreateExportIncentiveClaimCommandHandler CreateHandler(ApplicationDbContext ctx) =>
        new(new Repository<ExportIncentiveClaim, long>(ctx), new UnitOfWork(ctx),
            TestHarness.Numbering().Object, Posting(ctx));

    [Fact]
    public async Task Incentive_accrues_receivable_then_clears_on_receipt()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoa(ctx);

        var acc = await CreateHandler(ctx).Handle(new CreateExportIncentiveClaimCommand(
            null, "EXP-99", 4m, 4_000m, new DateOnly(2026, 6, 1), null), default);
        acc.Success.Should().BeTrue();
        Bal(ctx, "1186").Should().Be(4_000m);   // receivable
        Bal(ctx, "4260").Should().Be(-4_000m);  // income

        var id = ctx.ExportIncentiveClaims.Single().Id;
        var rec = await new MarkIncentiveReceivedCommandHandler(
            new Repository<ExportIncentiveClaim, long>(ctx), new UnitOfWork(ctx), Posting(ctx))
            .Handle(new MarkIncentiveReceivedCommand(id, new DateOnly(2026, 7, 1), "BankTransfer", "CR-1"), default);

        rec.Success.Should().BeTrue();
        ctx.ExportIncentiveClaims.Single().Status.Should().Be(IncentiveClaimStatus.Received);
        Bal(ctx, "1186").Should().Be(0m);       // receivable cleared
        Bal(ctx, "1120").Should().Be(4_000m);   // incentive banked
    }

    [Fact]
    public async Task Cancelling_an_accrued_incentive_reverses_it()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoa(ctx);
        await CreateHandler(ctx).Handle(new CreateExportIncentiveClaimCommand(
            null, "EXP-99", 4m, 4_000m, new DateOnly(2026, 6, 1), null), default);
        var id = ctx.ExportIncentiveClaims.Single().Id;

        var res = await new CancelExportIncentiveClaimCommandHandler(
            new Repository<ExportIncentiveClaim, long>(ctx), new UnitOfWork(ctx), Posting(ctx))
            .Handle(new CancelExportIncentiveClaimCommand(id), default);

        res.Success.Should().BeTrue();
        ctx.ExportIncentiveClaims.Single().Status.Should().Be(IncentiveClaimStatus.Cancelled);
        Bal(ctx, "1186").Should().Be(0m);
        Bal(ctx, "4260").Should().Be(0m);
    }
}
