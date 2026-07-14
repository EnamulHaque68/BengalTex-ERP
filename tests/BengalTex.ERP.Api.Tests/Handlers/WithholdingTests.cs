using BengalTex.ERP.Api.Tests.TestSupport;
using BengalTex.ERP.Application.Accounting.Statutory;
using BengalTex.ERP.Application.Payment.Commands;
using BengalTex.ERP.Application.Payment.Dtos;
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
/// Phase A5b — State / Withholding. Supplier payments deduct AIT/VDS at source (supplier receives
/// net, the tax is held in 2160/2170), delete reverses it, and statutory remittance clears the
/// payable on a challan (Dr payable / Cr Cash|Bank).
/// </summary>
public class WithholdingTests
{
    private static void SeedCoa(ApplicationDbContext ctx)
    {
        ctx.Accounts.AddRange(
            new Account { Id = 1, Code = "1110", Name = "Cash", AccountType = AccountType.Asset },
            new Account { Id = 2, Code = "1120", Name = "Bank", AccountType = AccountType.Asset },
            new Account { Id = 3, Code = "2110", Name = "AP", AccountType = AccountType.Liability },
            new Account { Id = 4, Code = "2160", Name = "AIT Payable", AccountType = AccountType.Liability },
            new Account { Id = 5, Code = "2170", Name = "VDS Payable", AccountType = AccountType.Liability },
            new Account { Id = 6, Code = "4300", Name = "Exchange Gain", AccountType = AccountType.Income },
            new Account { Id = 7, Code = "5800", Name = "Exchange Loss", AccountType = AccountType.Expense });
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

    private static long SeedApprovedInvoice(ApplicationDbContext ctx, decimal total = 10_000m)
    {
        var inv = new SupplierInvoice
        {
            Code = "SINV-1", SupplierId = 1, CurrencyId = 1, ExchangeRate = 1m,
            TotalAmount = total, AmountPaid = 0m, Status = SupplierInvoiceStatus.Approved
        };
        ctx.SupplierInvoices.Add(inv);
        ctx.SaveChanges();
        return inv.Id;
    }

    private static CreatePaymentCommandHandler PayHandler(ApplicationDbContext ctx)
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<Application.Payment.Queries.GetPaymentByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResponse<PaymentDto>.Ok(null!));
        return new CreatePaymentCommandHandler(
            new Repository<Payment, long>(ctx), new Repository<SupplierInvoice, long>(ctx),
            new UnitOfWork(ctx), TestHarness.Numbering().Object, Posting(ctx), mediator.Object);
    }

    // ── E4 — supplier withholding ──

    [Fact]
    public async Task Payment_with_withholding_splits_cash_and_holds_ait_vds()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoa(ctx);
        var invId = SeedApprovedInvoice(ctx);

        var res = await PayHandler(ctx).Handle(new CreatePaymentCommand(
            invId, new DateOnly(2026, 6, 10), 10_000m, "BankTransfer", null, null,
            ExchangeRate: null, AitAmount: 500m, VdsAmount: 300m), default);

        res.Success.Should().BeTrue();
        Bal(ctx, "2110").Should().Be(10_000m);    // AP fully cleared (Dr)
        Bal(ctx, "1120").Should().Be(-9_200m);    // supplier receives net cash
        Bal(ctx, "2160").Should().Be(-500m);      // AIT held
        Bal(ctx, "2170").Should().Be(-300m);      // VDS held
        ctx.Payments.Single().AitAmount.Should().Be(500m);
    }

    [Fact]
    public async Task Withholding_exceeding_payment_value_is_rejected()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoa(ctx);
        var invId = SeedApprovedInvoice(ctx);

        var res = await PayHandler(ctx).Handle(new CreatePaymentCommand(
            invId, new DateOnly(2026, 6, 10), 10_000m, "BankTransfer", null, null,
            ExchangeRate: null, AitAmount: 9_000m, VdsAmount: 2_000m), default);

        res.Success.Should().BeFalse();
        ctx.Payments.Should().BeEmpty();
    }

    [Fact]
    public async Task Deleting_a_withholding_payment_reverses_every_leg()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoa(ctx);
        var invId = SeedApprovedInvoice(ctx);
        await PayHandler(ctx).Handle(new CreatePaymentCommand(
            invId, new DateOnly(2026, 6, 10), 10_000m, "BankTransfer", null, null,
            ExchangeRate: null, AitAmount: 500m, VdsAmount: 300m), default);
        var payId = ctx.Payments.Single().Id;

        var del = await new DeletePaymentCommandHandler(
            new Repository<Payment, long>(ctx), new Repository<SupplierInvoice, long>(ctx),
            new UnitOfWork(ctx), Posting(ctx))
            .Handle(new DeletePaymentCommand(payId), default);

        del.Success.Should().BeTrue();
        Bal(ctx, "2110").Should().Be(0m);
        Bal(ctx, "1120").Should().Be(0m);
        Bal(ctx, "2160").Should().Be(0m);
        Bal(ctx, "2170").Should().Be(0m);
    }

    // ── E5 — statutory remittance ──

    [Fact]
    public async Task Remittance_clears_the_payable_against_bank()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoa(ctx);
        // Pre-existing AIT payable from a withholding: Cr 2160 500.
        var je = new JournalEntry { Code = "JV1", EntryDate = new DateOnly(2026, 6, 10), Status = JournalEntryStatus.Posted, PostedAt = DateTimeOffset.UtcNow, PostedBy = "t" };
        je.Lines.Add(new JournalEntryLine { AccountId = 4, Debit = 0m, Credit = 500m, SortOrder = 0 });
        je.Lines.Add(new JournalEntryLine { AccountId = 2, Debit = 500m, Credit = 0m, SortOrder = 1 });
        ctx.JournalEntries.Add(je);
        ctx.SaveChanges();

        Bal(ctx, "2160").Should().Be(-500m);   // outstanding before remittance

        var res = await new PostStatutoryRemittanceCommandHandler(
            new Repository<StatutoryRemittance, long>(ctx), new UnitOfWork(ctx),
            TestHarness.Numbering().Object, Posting(ctx))
            .Handle(new PostStatutoryRemittanceCommand(
                "Ait", 2026, 6, 500m, new DateOnly(2026, 7, 10), "BankTransfer", "CH-123", null), default);

        res.Success.Should().BeTrue();
        Bal(ctx, "2160").Should().Be(0m);      // payable cleared
        ctx.StatutoryRemittances.Single().ChallanNo.Should().Be("CH-123");
    }

    [Fact]
    public async Task Statutory_liabilities_report_returns_outstanding_per_type()
    {
        await using var ctx = TestHarness.NewContext();
        SeedCoa(ctx);
        var je = new JournalEntry { Code = "JV1", EntryDate = new DateOnly(2026, 6, 10), Status = JournalEntryStatus.Posted, PostedAt = DateTimeOffset.UtcNow, PostedBy = "t" };
        je.Lines.Add(new JournalEntryLine { AccountId = 4, Debit = 0m, Credit = 700m, SortOrder = 0 });   // AIT 700
        je.Lines.Add(new JournalEntryLine { AccountId = 5, Debit = 0m, Credit = 300m, SortOrder = 1 });   // VDS 300
        je.Lines.Add(new JournalEntryLine { AccountId = 2, Debit = 1_000m, Credit = 0m, SortOrder = 2 });
        ctx.JournalEntries.Add(je);
        ctx.SaveChanges();

        var res = await new GetStatutoryLiabilitiesQueryHandler(new Repository<JournalEntryLine, long>(ctx))
            .Handle(new GetStatutoryLiabilitiesQuery(new DateOnly(2026, 6, 30)), default);

        res.Success.Should().BeTrue();
        res.Data!.Items.Single(i => i.TaxType == "Ait").Outstanding.Should().Be(700m);
        res.Data.Items.Single(i => i.TaxType == "Vds").Outstanding.Should().Be(300m);
        res.Data.Items.Single(i => i.TaxType == "ProvidentFund").Outstanding.Should().Be(0m);
    }
}
