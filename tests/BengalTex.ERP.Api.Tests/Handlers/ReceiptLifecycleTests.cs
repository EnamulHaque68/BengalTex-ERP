using BengalTex.ERP.Api.Tests.TestSupport;
using BengalTex.ERP.Application.Receipt.Commands;
using BengalTex.ERP.Application.Receipt.Dtos;
using BengalTex.ERP.Application.Receipt.Queries;
using BengalTex.ERP.Application.Services;
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
/// Receipt Draft → Posted → Cancelled lifecycle. A draft never touches the invoice; only Post
/// reduces the outstanding (Unpaid → Partially Paid / Paid); Cancel reverses a posted receipt.
/// </summary>
public class ReceiptLifecycleTests
{
    private readonly Mock<IJournalPostingService> _journal = new();
    private readonly Mock<IMediator> _mediator = new();

    public ReceiptLifecycleTests()
    {
        _mediator.Setup(m => m.Send(It.IsAny<GetReceiptByIdQuery>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(ApiResponse<ReceiptDto>.Ok(null!));
    }

    private static async Task<long> SeedIssuedInvoice(ApplicationDbContext ctx, decimal total)
    {
        var inv = new CustomerInvoice
        {
            Code = "INV-1", CustomerId = 1, SalesOrderId = 1,
            InvoiceDate = new DateOnly(2026, 6, 1), DueDate = new DateOnly(2026, 6, 30),
            Status = CustomerInvoiceStatus.Issued,
            CurrencyId = 1, ExchangeRate = 1m,
            SubtotalAmount = total, VatAmount = 0m, TotalAmount = total, AmountPaid = 0m
        };
        ctx.CustomerInvoices.Add(inv);
        await ctx.SaveChangesAsync();
        return inv.Id;
    }

    private CreateReceiptCommandHandler CreateHandler(ApplicationDbContext ctx) =>
        new(new Repository<Receipt, long>(ctx),
            new Repository<CustomerInvoice, long>(ctx),
            new UnitOfWork(ctx), TestHarness.Numbering().Object, _mediator.Object);

    private PostReceiptCommandHandler PostHandler(ApplicationDbContext ctx) =>
        new(new Repository<Receipt, long>(ctx), new Repository<CustomerInvoice, long>(ctx),
            new UnitOfWork(ctx), new StubCurrentUser(), _journal.Object, _mediator.Object);

    private CancelReceiptCommandHandler CancelHandler(ApplicationDbContext ctx) =>
        new(new Repository<Receipt, long>(ctx), new Repository<CustomerInvoice, long>(ctx),
            new UnitOfWork(ctx), _journal.Object, _mediator.Object);

    [Fact]
    public async Task Draft_create_does_not_touch_the_invoice()
    {
        await using var ctx = TestHarness.NewContext();
        var invId = await SeedIssuedInvoice(ctx, 1000m);

        var res = await CreateHandler(ctx).Handle(
            new CreateReceiptCommand(invId, new DateOnly(2026, 6, 10), 400m, "Cash", null, null), default);

        res.Success.Should().BeTrue();
        var rct = ctx.Receipts.Single();
        rct.Status.Should().Be(ReceiptStatus.Draft);

        var inv = ctx.CustomerInvoices.Single();
        inv.AmountPaid.Should().Be(0m);                       // untouched
        inv.Status.Should().Be(CustomerInvoiceStatus.Issued); // still "Unpaid"
    }

    [Fact]
    public async Task Posting_applies_the_payment_and_moves_the_invoice_to_partially_paid()
    {
        await using var ctx = TestHarness.NewContext();
        var invId = await SeedIssuedInvoice(ctx, 1000m);
        await CreateHandler(ctx).Handle(
            new CreateReceiptCommand(invId, new DateOnly(2026, 6, 10), 400m, "Cash", null, null), default);
        var rctId = ctx.Receipts.Single().Id;

        var res = await PostHandler(ctx).Handle(new PostReceiptCommand(rctId), default);

        res.Success.Should().BeTrue();
        var rct = ctx.Receipts.Single();
        rct.Status.Should().Be(ReceiptStatus.Posted);
        rct.PostedAt.Should().NotBeNull();

        var inv = ctx.CustomerInvoices.Single();
        inv.AmountPaid.Should().Be(400m);
        inv.Status.Should().Be(CustomerInvoiceStatus.PartiallyPaid);
    }

    [Fact]
    public async Task Cancelling_a_posted_receipt_reverses_the_invoice()
    {
        await using var ctx = TestHarness.NewContext();
        var invId = await SeedIssuedInvoice(ctx, 1000m);
        await CreateHandler(ctx).Handle(
            new CreateReceiptCommand(invId, new DateOnly(2026, 6, 10), 1000m, "Cash", null, null), default);
        var rctId = ctx.Receipts.Single().Id;
        await PostHandler(ctx).Handle(new PostReceiptCommand(rctId), default);
        ctx.CustomerInvoices.Single().Status.Should().Be(CustomerInvoiceStatus.Paid); // sanity: fully paid

        var res = await CancelHandler(ctx).Handle(new CancelReceiptCommand(rctId), default);

        res.Success.Should().BeTrue();
        ctx.Receipts.Single().Status.Should().Be(ReceiptStatus.Cancelled);
        var inv = ctx.CustomerInvoices.Single();
        inv.AmountPaid.Should().Be(0m);
        inv.Status.Should().Be(CustomerInvoiceStatus.Issued); // back to "Unpaid"
    }

    [Fact]
    public async Task Posted_receipt_cannot_be_deleted()
    {
        await using var ctx = TestHarness.NewContext();
        var invId = await SeedIssuedInvoice(ctx, 1000m);
        await CreateHandler(ctx).Handle(
            new CreateReceiptCommand(invId, new DateOnly(2026, 6, 10), 200m, "Cash", null, null), default);
        var rctId = ctx.Receipts.Single().Id;
        await PostHandler(ctx).Handle(new PostReceiptCommand(rctId), default);

        var del = await new DeleteReceiptCommandHandler(new Repository<Receipt, long>(ctx), new UnitOfWork(ctx))
            .Handle(new DeleteReceiptCommand(rctId), default);

        del.Success.Should().BeFalse();
        del.Message.Should().Contain("cancel it first");
    }
}
