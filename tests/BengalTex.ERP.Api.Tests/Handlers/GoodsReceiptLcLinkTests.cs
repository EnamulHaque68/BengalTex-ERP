using BengalTex.ERP.Api.Tests.TestSupport;
using BengalTex.ERP.Application.GoodsReceipt.Commands;
using BengalTex.ERP.Application.GoodsReceipt.Dtos;
using BengalTex.ERP.Application.GoodsReceipt.Queries;
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
/// LC ↔ Goods Receipt integration: GRNs auto-link the PO's letter of credit and validate against it
/// (status + over-amount). Local / non-LC purchases are completely unaffected.
/// </summary>
public class GoodsReceiptLcLinkTests
{
    private readonly Mock<IMediator> _mediator = new();

    public GoodsReceiptLcLinkTests()
    {
        _mediator.Setup(m => m.Send(It.IsAny<GetGoodsReceiptByIdQuery>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(ApiResponse<GoodsReceiptDto>.Ok(null!));
    }

    private CreateGoodsReceiptCommandHandler Handler(ApplicationDbContext ctx) =>
        new(new Repository<GoodsReceiptNote, long>(ctx),
            new Repository<PurchaseOrder, long>(ctx),
            new Repository<Warehouse>(ctx),
            new Repository<LetterOfCredit, long>(ctx),
            new UnitOfWork(ctx), TestHarness.Numbering().Object, _mediator.Object);

    /// <summary>Seeds an Approved PO (1 line: qty 100 @ price 10) + masters; optionally a linked LC.</summary>
    private static async Task<(long PoId, long PoLineId)> SeedPo(
        ApplicationDbContext ctx, decimal lcAmount = 0m, LcStatus lcStatus = LcStatus.Open, bool withLc = false)
    {
        ctx.Suppliers.Add(new Supplier { Id = 1, Code = "S-1", Name = "Yarn Mills" });
        ctx.Currencies.Add(new Currency { Id = 1, Code = "USD", Name = "Dollar", Symbol = "$", ExchangeRateToBase = 127m });
        ctx.RawMaterials.Add(new RawMaterial { Id = 1, Code = "RM-1", Name = "Cotton", UnitOfMeasureId = 1 });
        ctx.Warehouses.Add(new Warehouse { Id = 1, Code = "WH-1", Name = "Main" });
        var po = new PurchaseOrder
        {
            Code = "PO-1", SupplierId = 1, CurrencyId = 1, ExchangeRate = 127m,
            Status = PurchaseOrderStatus.Approved, OrderDate = new DateOnly(2026, 6, 1),
            Lines = { new PurchaseOrderLine { RawMaterialId = 1, Quantity = 100m, UnitPrice = 10m } }
        };
        ctx.PurchaseOrders.Add(po);
        await ctx.SaveChangesAsync();

        if (withLc)
        {
            ctx.LettersOfCredit.Add(new LetterOfCredit
            {
                Code = "LC-1", LcNumber = "LCN-1", IssuingBank = "BRAC", SupplierId = 1,
                PurchaseOrderId = po.Id, CurrencyId = 1, ExchangeRate = 127m, Amount = lcAmount,
                IssueDate = new DateOnly(2026, 6, 1), ExpiryDate = new DateOnly(2026, 12, 1),
                TenorDays = 90, Status = lcStatus, Type = LcType.Import
            });
            await ctx.SaveChangesAsync();
        }
        return (po.Id, po.Lines.First().Id);
    }

    private static CreateGoodsReceiptCommand Receive(long poId, long poLineId, decimal qty, long? lcId = null) =>
        new(poId, new DateOnly(2026, 6, 10), 1, null, null,
            new[] { new GoodsReceiptLineInput(poLineId, qty, null) }, lcId);

    [Fact]
    public async Task Non_lc_purchase_receives_normally()
    {
        await using var ctx = TestHarness.NewContext();
        var (poId, poLineId) = await SeedPo(ctx, withLc: false);

        var res = await Handler(ctx).Handle(Receive(poId, poLineId, 50m), default);

        res.Success.Should().BeTrue();
        ctx.GoodsReceiptNotes.Single().LetterOfCreditId.Should().BeNull();
    }

    [Fact]
    public async Task Auto_links_the_pos_open_lc()
    {
        await using var ctx = TestHarness.NewContext();
        var (poId, poLineId) = await SeedPo(ctx, lcAmount: 1000m, lcStatus: LcStatus.Open, withLc: true);

        var res = await Handler(ctx).Handle(Receive(poId, poLineId, 50m), default);  // value 500 ≤ 1000

        res.Success.Should().BeTrue();
        var lcId = ctx.LettersOfCredit.Single().Id;
        ctx.GoodsReceiptNotes.Single().LetterOfCreditId.Should().Be(lcId);
    }

    [Fact]
    public async Task Blocks_receipt_against_a_settled_lc()
    {
        await using var ctx = TestHarness.NewContext();
        var (poId, poLineId) = await SeedPo(ctx, lcAmount: 1000m, lcStatus: LcStatus.Settled, withLc: true);
        var lcId = ctx.LettersOfCredit.Single().Id;

        var res = await Handler(ctx).Handle(Receive(poId, poLineId, 10m, lcId), default);

        res.Success.Should().BeFalse();
        res.Message.Should().Contain("Settled");
        ctx.GoodsReceiptNotes.Should().BeEmpty();
    }

    [Fact]
    public async Task Blocks_receipt_over_the_lc_amount()
    {
        await using var ctx = TestHarness.NewContext();
        // LC amount 400; receiving 50 @ 10 = value 500 > 400 (PO remaining 100 ≥ 50, so PO check passes).
        var (poId, poLineId) = await SeedPo(ctx, lcAmount: 400m, lcStatus: LcStatus.Open, withLc: true);

        var res = await Handler(ctx).Handle(Receive(poId, poLineId, 50m), default);

        res.Success.Should().BeFalse();
        res.Message.Should().Contain("exceed");
        ctx.GoodsReceiptNotes.Should().BeEmpty();
    }
}
