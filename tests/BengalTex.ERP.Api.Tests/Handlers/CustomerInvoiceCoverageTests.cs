using BengalTex.ERP.Api.Tests.TestSupport;
using BengalTex.ERP.Application.CustomerInvoice.Commands;
using BengalTex.ERP.Application.CustomerInvoice.Dtos;
using BengalTex.ERP.Application.CustomerInvoice.Queries;
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
/// Customer-invoice ↔ Sales-order coverage: a line that links to an SO line consumes its
/// remaining-to-invoice (Quantity − InvoicedQuantity), over-cap is blocked (duplicate / full-invoice
/// prevention), and cancelling releases the quantity back.
/// </summary>
public class CustomerInvoiceCoverageTests
{
    private readonly Mock<IMediator> _mediator = new();

    public CustomerInvoiceCoverageTests()
    {
        _mediator.Setup(m => m.Send(It.IsAny<GetCustomerInvoiceByIdQuery>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(ApiResponse<CustomerInvoiceDto>.Ok(null!));
    }

    private CreateCustomerInvoiceCommandHandler CreateHandler(ApplicationDbContext ctx) =>
        new(new Repository<CustomerInvoice, long>(ctx),
            new Repository<SalesOrder, long>(ctx),
            new Repository<SalesOrderLine, long>(ctx),
            new Repository<Customer>(ctx),
            new Repository<Product>(ctx),
            new UnitOfWork(ctx), TestHarness.Numbering().Object, _mediator.Object);

    private CancelCustomerInvoiceCommandHandler CancelHandler(ApplicationDbContext ctx) =>
        new(new Repository<CustomerInvoice, long>(ctx),
            new Repository<VatChallan, long>(ctx),
            new Repository<SalesOrderLine, long>(ctx),
            new UnitOfWork(ctx),
            new Mock<IJournalPostingService>().Object,
            _mediator.Object);

    /// <summary>Seeds a Confirmed SO with one line (qty 100) + its customer/currency/product.</summary>
    private static async Task<(long SoId, long SoLineId)> SeedSo(ApplicationDbContext ctx)
    {
        ctx.Customers.Add(new Customer { Id = 1, Code = "C-1", Name = "Acme", CreditPeriodDays = 30 });
        ctx.Currencies.Add(new Currency { Id = 1, Code = "BDT", Name = "Taka", Symbol = "BDT", ExchangeRateToBase = 1m });
        ctx.Products.Add(new Product { Id = 5, Code = "P-5", Name = "Tag", UnitOfMeasureId = 1 });
        var so = new SalesOrder
        {
            Code = "SO-1", CustomerId = 1, CurrencyId = 1, ExchangeRate = 1m,
            Status = SalesOrderStatus.Confirmed,
            Lines = { new SalesOrderLine { ProductId = 5, Quantity = 100m, UnitPrice = 10m } }
        };
        ctx.SalesOrders.Add(so);
        await ctx.SaveChangesAsync();
        return (so.Id, so.Lines.First().Id);
    }

    private static CreateCustomerInvoiceCommand Invoice(long soId, long soLineId, decimal qty) =>
        new(soId, 0m, new DateOnly(2026, 6, 1), null, null,
            new[] { new CustomerInvoiceLineInput(5, qty, 10m, null, soLineId) });

    [Fact]
    public async Task Invoicing_consumes_the_so_line_remaining()
    {
        await using var ctx = TestHarness.NewContext();
        var (soId, soLineId) = await SeedSo(ctx);

        var res = await CreateHandler(ctx).Handle(Invoice(soId, soLineId, 60m), default);

        res.Success.Should().BeTrue();
        ctx.SalesOrderLines.Single().InvoicedQuantity.Should().Be(60m);
    }

    [Fact]
    public async Task Over_remaining_invoice_is_blocked()
    {
        await using var ctx = TestHarness.NewContext();
        var (soId, soLineId) = await SeedSo(ctx);
        await CreateHandler(ctx).Handle(Invoice(soId, soLineId, 60m), default);   // 40 remains

        var res = await CreateHandler(ctx).Handle(Invoice(soId, soLineId, 50m), default);

        res.Success.Should().BeFalse();
        res.Message.Should().Contain("40");
        ctx.SalesOrderLines.Single().InvoicedQuantity.Should().Be(60m);   // unchanged
    }

    [Fact]
    public async Task Fully_invoiced_then_blocked_then_cancel_frees_it()
    {
        await using var ctx = TestHarness.NewContext();
        var (soId, soLineId) = await SeedSo(ctx);
        var created = await CreateHandler(ctx).Handle(Invoice(soId, soLineId, 100m), default);
        ctx.SalesOrderLines.Single().InvoicedQuantity.Should().Be(100m);

        // Fully invoiced → any further invoice blocked.
        var blocked = await CreateHandler(ctx).Handle(Invoice(soId, soLineId, 1m), default);
        blocked.Success.Should().BeFalse();

        // Cancel releases the coverage — the SO line is invoiceable again.
        var invId = ctx.CustomerInvoices.OrderBy(i => i.Id).First().Id;
        var cancelled = await CancelHandler(ctx).Handle(new CancelCustomerInvoiceCommand(invId), default);

        cancelled.Success.Should().BeTrue();
        ctx.SalesOrderLines.Single().InvoicedQuantity.Should().Be(0m);
    }
}
