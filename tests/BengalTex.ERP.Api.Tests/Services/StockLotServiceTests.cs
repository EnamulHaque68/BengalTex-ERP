using BengalTex.ERP.Api.Tests.TestSupport;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Infrastructure.Persistence;
using BengalTex.ERP.Infrastructure.Persistence.Repositories;
using BengalTex.ERP.Infrastructure.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace BengalTex.ERP.Api.Tests.Services;

public class StockLotServiceTests
{
    private const int Rm = 1;
    private const int Wh = 1;

    private static StockLot Lot(int rmId, int whId, decimal qty, DateOnly received, LotStatus status = LotStatus.Active) => new()
    {
        Code = $"LOT-{Guid.NewGuid():N}",
        LotNumber = "B" + received.DayNumber,
        RawMaterialId = rmId,
        WarehouseId = whId,
        ReceivedDate = received,
        InitialQuantity = qty,
        CurrentQuantity = qty,
        Status = status
    };

    /// <summary>Builds the service over an InMemory context, recording every movement the
    /// (mocked) IStockService is asked to post as a (signedQuantity, lot) pair.</summary>
    private static (StockLotService svc, ApplicationDbContext ctx, List<(decimal qty, StockLot? lot)> posted)
        Build()
    {
        var ctx = TestHarness.NewContext();
        var posted = new List<(decimal, StockLot?)>();
        var stock = new Mock<IStockService>();
        stock.Setup(s => s.PostRawMaterialMovementAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<StockMovementType>(),
                It.IsAny<string?>(), It.IsAny<long?>(), It.IsAny<string?>(), It.IsAny<DateOnly>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>(), It.IsAny<StockLot?>()))
            .Callback(new InvocationAction(inv => posted.Add(((decimal)inv.Arguments[2], (StockLot?)inv.Arguments[10]))))
            .Returns(Task.CompletedTask);

        var svc = new StockLotService(new Repository<StockLot, long>(ctx), stock.Object);
        return (svc, ctx, posted);
    }

    private static Task Consume(StockLotService svc, decimal qty) =>
        svc.ConsumeRawMaterialFifoAsync(Rm, Wh, qty, StockMovementType.ProductionIssue,
            "ProductionOrder", 99, "PRD-1", new DateOnly(2026, 5, 22), null);

    [Fact]
    public async Task Consumes_oldest_lots_first_and_marks_depleted()
    {
        var (svc, ctx, posted) = Build();
        var older = Lot(Rm, Wh, 60m, new DateOnly(2026, 5, 1));
        var newer = Lot(Rm, Wh, 40m, new DateOnly(2026, 5, 10));
        ctx.StockLots.AddRange(newer, older);   // add out of order — service must still pick oldest first
        await ctx.SaveChangesAsync();

        await Consume(svc, 80m);
        await ctx.SaveChangesAsync();

        older.CurrentQuantity.Should().Be(0m);
        older.Status.Should().Be(LotStatus.Depleted);
        newer.CurrentQuantity.Should().Be(20m);
        newer.Status.Should().Be(LotStatus.Active);

        posted.Should().HaveCount(2);
        posted[0].Should().Be((-60m, older));    // full older lot first
        posted[1].Should().Be((-20m, newer));    // remainder from newer
    }

    [Fact]
    public async Task Quantity_beyond_lots_posts_an_untagged_remainder()
    {
        var (svc, ctx, posted) = Build();
        var lot = Lot(Rm, Wh, 30m, new DateOnly(2026, 5, 1));
        ctx.StockLots.Add(lot);
        await ctx.SaveChangesAsync();

        await Consume(svc, 50m);
        await ctx.SaveChangesAsync();

        lot.CurrentQuantity.Should().Be(0m);
        lot.Status.Should().Be(LotStatus.Depleted);

        posted.Should().HaveCount(2);
        posted[0].Should().Be((-30m, lot));      // all of the lot
        posted[1].qty.Should().Be(-20m);
        posted[1].lot.Should().BeNull();          // un-tagged remainder
    }

    [Fact]
    public async Task No_lots_posts_a_single_untagged_movement()
    {
        var (svc, _, posted) = Build();

        await Consume(svc, 25m);

        posted.Should().ContainSingle();
        posted[0].qty.Should().Be(-25m);
        posted[0].lot.Should().BeNull();
    }

    [Fact]
    public async Task Skips_depleted_lots_and_uses_only_active_stock()
    {
        var (svc, ctx, posted) = Build();
        var depleted = Lot(Rm, Wh, 0m, new DateOnly(2026, 4, 1), LotStatus.Depleted);
        var active = Lot(Rm, Wh, 50m, new DateOnly(2026, 5, 1));
        ctx.StockLots.AddRange(depleted, active);
        await ctx.SaveChangesAsync();

        await Consume(svc, 20m);
        await ctx.SaveChangesAsync();

        active.CurrentQuantity.Should().Be(30m);
        posted.Should().ContainSingle();
        posted[0].Should().Be((-20m, active));
    }

    [Fact]
    public async Task Zero_quantity_is_a_no_op()
    {
        var (svc, ctx, posted) = Build();
        ctx.StockLots.Add(Lot(Rm, Wh, 10m, new DateOnly(2026, 5, 1)));
        await ctx.SaveChangesAsync();

        await Consume(svc, 0m);

        posted.Should().BeEmpty();
    }
}
