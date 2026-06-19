using BengalTex.ERP.Api.Tests.TestSupport;
using BengalTex.ERP.Application.Accounting;
using BengalTex.ERP.Application.LandedCost;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Infrastructure.Persistence;
using BengalTex.ERP.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Moq;
using Xunit;

namespace BengalTex.ERP.Api.Tests.Handlers;

/// <summary>
/// Landed-cost posting capitalises import charges onto a posted GRN's raw materials: it spreads
/// the total charges across the receipt lines (by value here), raises each RM's WAC by its share ÷
/// current on-hand, and journals Dr Raw Material Inventory (absorbed) + Dr COGS (un-absorbable, when
/// stock was already consumed) / Cr Cash|Bank. Value-only — no stock movement.
/// </summary>
public class LandedCostPostTests
{
    private readonly Mock<IStockService> _stock = new();
    private readonly Mock<IJournalPostingService> _journal = new();
    private readonly List<(string Account, decimal Debit, decimal Credit)[]> _journalCalls = new();

    public LandedCostPostTests()
    {
        _journal.Setup(j => j.PostAsync(
                It.IsAny<DateOnly>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(),
                It.IsAny<string>(), It.IsAny<IReadOnlyList<JournalPostingLine>>(), It.IsAny<CancellationToken>()))
            .Callback<DateOnly, string, string, long, string, IReadOnlyList<JournalPostingLine>, CancellationToken>(
                (_, _, _, _, _, lines, _) =>
                    _journalCalls.Add(lines.Select(l => (l.AccountCode, l.Debit, l.Credit)).ToArray()))
            .Returns(Task.CompletedTask);
    }

    private PostLandedCostVoucherCommandHandler Handler(ApplicationDbContext ctx) =>
        new(new Repository<LandedCostVoucher, long>(ctx), _stock.Object, _journal.Object,
            new StubCurrentUser(), new UnitOfWork(ctx));

    // GRN with two lines of equal value (100×5 and 50×10 = 500 each) against a 200 freight charge.
    private static void SeedGrnWithVoucher(ApplicationDbContext ctx, decimal rm10Wac, decimal rm20Wac)
    {
        ctx.RawMaterials.Add(new RawMaterial { Id = 10, Code = "RM-10", Name = "Yarn", UnitOfMeasureId = 1, WeightedAverageCost = rm10Wac });
        ctx.RawMaterials.Add(new RawMaterial { Id = 20, Code = "RM-20", Name = "Dye", UnitOfMeasureId = 1, WeightedAverageCost = rm20Wac });
        ctx.PurchaseOrders.Add(new PurchaseOrder
        {
            Id = 1, Code = "PO-1", SupplierId = 1, CurrencyId = 1, ExchangeRate = 1m,
            Status = PurchaseOrderStatus.Received,
            Lines =
            {
                new PurchaseOrderLine { Id = 100, RawMaterialId = 10, Quantity = 100m, UnitPrice = 5m, ReceivedQuantity = 100m },
                new PurchaseOrderLine { Id = 200, RawMaterialId = 20, Quantity = 50m, UnitPrice = 10m, ReceivedQuantity = 50m }
            }
        });
        ctx.GoodsReceiptNotes.Add(new GoodsReceiptNote
        {
            Id = 1, Code = "GRN-1", PurchaseOrderId = 1, ReceiveDate = new DateOnly(2026, 6, 1),
            ReceivingWarehouseId = 1, Status = GoodsReceiptStatus.Posted,
            Lines =
            {
                new GoodsReceiptLine { PurchaseOrderLineId = 100, ReceivedQuantity = 100m, SortOrder = 0 },
                new GoodsReceiptLine { PurchaseOrderLineId = 200, ReceivedQuantity = 50m, SortOrder = 1 }
            }
        });
        ctx.LandedCostVouchers.Add(new LandedCostVoucher
        {
            Id = 1, Code = "LCV-1", VoucherDate = new DateOnly(2026, 6, 2), GoodsReceiptNoteId = 1,
            AllocationBasis = LandedCostAllocationBasis.ByValue, PaymentMethod = PaymentMethod.BankTransfer,
            Status = LandedCostVoucherStatus.Draft,
            Charges = { new LandedCostCharge { ChargeType = LandedCostChargeType.Freight, Amount = 200m, SortOrder = 0 } }
        });
    }

    [Fact]
    public async Task Absorbs_charges_into_wac_and_journals_to_raw_material_inventory()
    {
        await using var ctx = TestHarness.NewContext();
        SeedGrnWithVoucher(ctx, rm10Wac: 5m, rm20Wac: 10m);
        await ctx.SaveChangesAsync();
        // both materials still fully on hand
        _stock.Setup(s => s.GetRawMaterialTotalOnHandAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(100m);
        _stock.Setup(s => s.GetRawMaterialTotalOnHandAsync(20, It.IsAny<CancellationToken>())).ReturnsAsync(50m);

        var result = await Handler(ctx).Handle(new PostLandedCostVoucherCommand(1), default);

        result.Success.Should().BeTrue();
        // equal line value → 100 to each line; WAC bump = alloc ÷ onHand
        ctx.RawMaterials.Single(r => r.Id == 10).WeightedAverageCost.Should().Be(6m);   // 5 + 100/100
        ctx.RawMaterials.Single(r => r.Id == 20).WeightedAverageCost.Should().Be(12m);  // 10 + 100/50

        var legs = _journalCalls.Should().ContainSingle().Subject;
        legs.Single(l => l.Account == LedgerAccounts.RawMaterialInventory).Debit.Should().Be(200m);
        legs.Single(l => l.Account == LedgerAccounts.CostOfGoodsSold).Debit.Should().Be(0m);
        legs.Single(l => l.Account == LedgerAccounts.Bank).Credit.Should().Be(200m);

        ctx.LandedCostVouchers.Single().Status.Should().Be(LandedCostVoucherStatus.Posted);
    }

    [Fact]
    public async Task Unabsorbable_share_for_consumed_stock_goes_to_cogs()
    {
        await using var ctx = TestHarness.NewContext();
        SeedGrnWithVoucher(ctx, rm10Wac: 5m, rm20Wac: 10m);
        await ctx.SaveChangesAsync();
        // RM-10 fully consumed since receipt (0 on hand) → its 100 share can't be capitalised
        _stock.Setup(s => s.GetRawMaterialTotalOnHandAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(0m);
        _stock.Setup(s => s.GetRawMaterialTotalOnHandAsync(20, It.IsAny<CancellationToken>())).ReturnsAsync(50m);

        var result = await Handler(ctx).Handle(new PostLandedCostVoucherCommand(1), default);

        result.Success.Should().BeTrue();
        ctx.RawMaterials.Single(r => r.Id == 10).WeightedAverageCost.Should().Be(5m);   // unchanged (no stock)
        ctx.RawMaterials.Single(r => r.Id == 20).WeightedAverageCost.Should().Be(12m);  // 10 + 100/50

        var legs = _journalCalls[0];
        legs.Single(l => l.Account == LedgerAccounts.RawMaterialInventory).Debit.Should().Be(100m);
        legs.Single(l => l.Account == LedgerAccounts.CostOfGoodsSold).Debit.Should().Be(100m);
        legs.Single(l => l.Account == LedgerAccounts.Bank).Credit.Should().Be(200m);
    }
}
