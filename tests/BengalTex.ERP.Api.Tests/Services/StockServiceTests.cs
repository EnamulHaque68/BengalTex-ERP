using BengalTex.ERP.Api.Tests.TestSupport;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Infrastructure.Persistence;
using BengalTex.ERP.Infrastructure.Persistence.Repositories;
using BengalTex.ERP.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BengalTex.ERP.Api.Tests.Services;

public class StockServiceTests
{
    private static (StockService svc, ApplicationDbContext ctx) Build()
    {
        var ctx = TestHarness.NewContext();
        var svc = new StockService(
            new Repository<StockMovement, long>(ctx),
            new Repository<StockOnHand>(ctx),
            TestHarness.Numbering().Object);
        return (svc, ctx);
    }

    [Fact]
    public async Task First_inbound_creates_movement_and_stock_on_hand()
    {
        var (svc, ctx) = Build();

        await svc.PostRawMaterialMovementAsync(
            rawMaterialId: 5, warehouseId: 2, signedQuantity: 100m,
            movementType: StockMovementType.GrnReceipt,
            referenceType: "GRN", referenceId: 1, referenceCode: "GRN-1",
            movementDate: new DateOnly(2026, 5, 22), notes: null);
        await ctx.SaveChangesAsync();

        var movement = await ctx.StockMovements.SingleAsync();
        movement.RawMaterialId.Should().Be(5);
        movement.SignedQuantity.Should().Be(100m);
        movement.MovementType.Should().Be(StockMovementType.GrnReceipt);
        movement.LotId.Should().BeNull();

        var soh = await ctx.StockOnHand.SingleAsync();
        soh.RawMaterialId.Should().Be(5);
        soh.WarehouseId.Should().Be(2);
        soh.Quantity.Should().Be(100m);
    }

    [Fact]
    public async Task Subsequent_movements_accumulate_into_one_stock_on_hand_row()
    {
        var (svc, ctx) = Build();

        // Two separate operations (GRN post, then production issue) — each commits on its own,
        // mirroring how the caller owns SaveChanges. The second upsert finds + increments the first.
        await svc.PostRawMaterialMovementAsync(5, 2, 100m, StockMovementType.GrnReceipt, "GRN", 1, "GRN-1", new DateOnly(2026, 5, 22), null);
        await ctx.SaveChangesAsync();
        await svc.PostRawMaterialMovementAsync(5, 2, -30m, StockMovementType.ProductionIssue, "PRD", 2, "PRD-1", new DateOnly(2026, 5, 22), null);
        await ctx.SaveChangesAsync();

        (await ctx.StockMovements.CountAsync()).Should().Be(2);
        var soh = await ctx.StockOnHand.SingleAsync();   // still exactly one row for (RM,WH)
        soh.Quantity.Should().Be(70m);
    }

    [Fact]
    public async Task Different_warehouse_gets_its_own_stock_row()
    {
        var (svc, ctx) = Build();

        await svc.PostRawMaterialMovementAsync(5, 2, 100m, StockMovementType.GrnReceipt, "GRN", 1, "GRN-1", new DateOnly(2026, 5, 22), null);
        await svc.PostRawMaterialMovementAsync(5, 3, 40m, StockMovementType.GrnReceipt, "GRN", 1, "GRN-2", new DateOnly(2026, 5, 22), null);
        await ctx.SaveChangesAsync();

        (await ctx.StockOnHand.CountAsync()).Should().Be(2);
        (await svc.GetRawMaterialOnHandAsync(5, 2)).Should().Be(100m);
        (await svc.GetRawMaterialOnHandAsync(5, 3)).Should().Be(40m);
        (await svc.GetRawMaterialTotalOnHandAsync(5)).Should().Be(140m);   // summed across warehouses
    }

    [Fact]
    public async Task Movement_is_tagged_with_the_lot_via_navigation()
    {
        var (svc, ctx) = Build();
        var lot = new StockLot
        {
            Code = "LOT-1", LotNumber = "B1", RawMaterialId = 5, WarehouseId = 2,
            ReceivedDate = new DateOnly(2026, 5, 1), InitialQuantity = 100m, CurrentQuantity = 100m,
            Status = LotStatus.Active
        };
        ctx.StockLots.Add(lot);
        await ctx.SaveChangesAsync();

        await svc.PostRawMaterialMovementAsync(5, 2, 100m, StockMovementType.GrnReceipt, "GRN", 1, "GRN-1",
            new DateOnly(2026, 5, 22), null, ct: default, lot: lot);
        await ctx.SaveChangesAsync();

        var movement = await ctx.StockMovements.SingleAsync();
        movement.LotId.Should().Be(lot.Id);
        lot.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Product_movement_creates_a_product_stock_row()
    {
        var (svc, ctx) = Build();

        await svc.PostProductMovementAsync(7, 4, 25m, StockMovementType.ProductionReceipt, "PRD", 1, "PRD-1", new DateOnly(2026, 5, 22), null);
        await ctx.SaveChangesAsync();

        var soh = await ctx.StockOnHand.SingleAsync();
        soh.ProductId.Should().Be(7);
        soh.RawMaterialId.Should().BeNull();
        soh.Quantity.Should().Be(25m);
    }

    [Fact]
    public async Task Zero_quantity_movement_is_rejected()
    {
        var (svc, _) = Build();

        var act = () => svc.PostRawMaterialMovementAsync(5, 2, 0m, StockMovementType.GrnReceipt, "GRN", 1, "GRN-1", new DateOnly(2026, 5, 22), null);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
