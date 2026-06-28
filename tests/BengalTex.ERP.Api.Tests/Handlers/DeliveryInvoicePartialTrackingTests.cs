using BengalTex.ERP.Api.Tests.TestSupport;
using BengalTex.ERP.Application.CustomerInvoice.Commands;
using BengalTex.ERP.Application.CustomerInvoice.Dtos;
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
/// DN → Invoice partial-invoice tracking: only the remaining (Dispatched − Invoiced) quantity of
/// each delivery-note line can be billed. The handler validates the request, increments
/// <see cref="DeliveryNoteLine.InvoicedQuantity"/>, and refuses over-remaining / fully-invoiced
/// requests. The actual invoice document is created by the inner CreateCustomerInvoiceCommand
/// (mocked here — we only assert the guard + the running-total bookkeeping).
/// </summary>
public class DeliveryInvoicePartialTrackingTests
{
    private static (CreateInvoiceFromDeliveryNoteCommandHandler Handler, Mock<IMediator> Mediator)
        Build(ApplicationDbContext ctx)
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<CreateCustomerInvoiceCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ApiResponse<CustomerInvoiceDto>.Ok(null!));
        var handler = new CreateInvoiceFromDeliveryNoteCommandHandler(
            new Repository<DeliveryNote, long>(ctx), mediator.Object);
        return (handler, mediator);
    }

    /// <summary>Seeds one posted DN whose single line dispatched 100 (already-invoiced configurable).</summary>
    private static async Task<(long DnId, long LineId)> SeedPostedDn(
        ApplicationDbContext ctx, decimal dispatched, decimal alreadyInvoiced)
    {
        var soLine = new SalesOrderLine { SalesOrderId = 1, ProductId = 7, Quantity = 100m, UnitPrice = 5m };
        ctx.Add(soLine);
        await ctx.SaveChangesAsync();

        var dn = new DeliveryNote
        {
            Code = "DN-1", SalesOrderId = 1, DispatchWarehouseId = 1,
            Status = DeliveryNoteStatus.Posted,
            Lines =
            {
                new DeliveryNoteLine
                {
                    SalesOrderLineId = soLine.Id,
                    DispatchedQuantity = dispatched,
                    InvoicedQuantity = alreadyInvoiced
                }
            }
        };
        ctx.DeliveryNotes.Add(dn);
        await ctx.SaveChangesAsync();
        return (dn.Id, dn.Lines.First().Id);
    }

    [Fact]
    public async Task Blocks_invoicing_more_than_the_remaining_quantity()
    {
        await using var ctx = TestHarness.NewContext();
        var (dnId, lineId) = await SeedPostedDn(ctx, dispatched: 100m, alreadyInvoiced: 40m); // 60 remains
        var (handler, mediator) = Build(ctx);

        var result = await handler.Handle(new CreateInvoiceFromDeliveryNoteCommand(
            dnId, 0m, new[] { new DeliveryInvoiceLineInput(lineId, 61m) }), default);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("only 60");
        mediator.Verify(m => m.Send(It.IsAny<CreateCustomerInvoiceCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Blocks_a_fully_invoiced_delivery_note()
    {
        await using var ctx = TestHarness.NewContext();
        var (dnId, _) = await SeedPostedDn(ctx, dispatched: 100m, alreadyInvoiced: 100m); // nothing remains
        var (handler, mediator) = Build(ctx);

        // No explicit lines → "invoice all remaining" path, which is now zero.
        var result = await handler.Handle(new CreateInvoiceFromDeliveryNoteCommand(dnId), default);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("fully invoiced");
        mediator.Verify(m => m.Send(It.IsAny<CreateCustomerInvoiceCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Partial_invoice_increments_the_running_invoiced_total()
    {
        await using var ctx = TestHarness.NewContext();
        var (dnId, lineId) = await SeedPostedDn(ctx, dispatched: 100m, alreadyInvoiced: 0m);
        var (handler, mediator) = Build(ctx);

        var result = await handler.Handle(new CreateInvoiceFromDeliveryNoteCommand(
            dnId, 0m, new[] { new DeliveryInvoiceLineInput(lineId, 30m) }), default);

        result.Success.Should().BeTrue();
        mediator.Verify(m => m.Send(It.IsAny<CreateCustomerInvoiceCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        ctx.DeliveryNotes.Single().Lines.Single().InvoicedQuantity.Should().Be(30m); // 0 → 30
    }
}
