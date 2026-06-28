using BengalTex.ERP.Api.Tests.TestSupport;
using BengalTex.ERP.Application.Production.Commands;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Infrastructure.Persistence;
using BengalTex.ERP.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace BengalTex.ERP.Api.Tests.Handlers;

/// <summary>The additive multi-stage gate: an InProgress order with a routing can't be
/// completed until every stage is Completed/Skipped — but orders with a finished (or no)
/// routing still flow through to the normal BOM/stock logic.</summary>
public class CompleteProductionOrderGateTests
{
    private static CompleteProductionOrderCommandHandler Handler(ApplicationDbContext ctx) =>
        new(new Repository<ProductionOrder, long>(ctx),
            new BengalTex.ERP.Infrastructure.Persistence.UnitOfWork(ctx),
            new Mock<IStockService>().Object,
            new Mock<IStockLotService>().Object,
            new Mock<IStockReservationService>().Object,
            new Mock<IJournalPostingService>().Object,
            new StubCurrentUser(),
            new Mock<INotificationService>().Object,
            new Mock<IMediator>().Object);

    private static ProductionOrder InProgressOrder() => new()
    {
        Code = "PRD-1", ProductId = 1, BomId = 1, Quantity = 100m,
        IssueWarehouseId = 1, ReceiveWarehouseId = 1,
        Status = ProductionOrderStatus.InProgress
    };

    // The Complete query Includes the required Product + Bom navigations; EF InMemory drops the
    // order from results if those principals are missing, so seed them. Bom has no lines.
    private static void SeedProductAndBom(ApplicationDbContext ctx)
    {
        ctx.Products.Add(new Product { Id = 1, Code = "P-1", Name = "Tag", UnitOfMeasureId = 1 });
        ctx.Boms.Add(new Bom { Id = 1, Code = "BOM-1", ProductId = 1, Version = 1, OutputQuantity = 1m });
    }

    private static ProductionStage Stage(int seq, string name, ProductionStageStatus status) => new()
    {
        Sequence = seq, StageName = name, Status = status,
        PlannedQuantity = 100m, CompletedQuantity = 0m, RejectedQuantity = 0m
    };

    [Fact]
    public async Task Blocks_completion_while_a_stage_is_unfinished()
    {
        await using var ctx = TestHarness.NewContext();
        var order = InProgressOrder();
        order.Stages.Add(Stage(1, "Cutting", ProductionStageStatus.Completed));
        order.Stages.Add(Stage(2, "Sewing", ProductionStageStatus.Pending));
        ctx.ProductionOrders.Add(order);
        SeedProductAndBom(ctx);
        await ctx.SaveChangesAsync();

        var result = await Handler(ctx).Handle(new CompleteProductionOrderCommand(order.Id), default);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("stage");
        result.Message.Should().Contain("Sewing");   // names the pending stage
    }

    [Fact]
    public async Task Lets_completion_proceed_once_every_stage_is_done()
    {
        await using var ctx = TestHarness.NewContext();
        var order = InProgressOrder();
        order.Stages.Add(Stage(1, "Cutting", ProductionStageStatus.Completed));
        order.Stages.Add(Stage(2, "Sewing", ProductionStageStatus.Skipped));
        ctx.ProductionOrders.Add(order);
        // BOM has no lines, so once the gate passes the failure is about the BOM, not a stage.
        SeedProductAndBom(ctx);
        await ctx.SaveChangesAsync();

        var result = await Handler(ctx).Handle(new CompleteProductionOrderCommand(order.Id), default);

        result.Success.Should().BeFalse();
        result.Message.Should().NotContain("stage");   // gate passed — failure is now about the BOM
        result.Message!.ToLowerInvariant().Should().Contain("bom");
    }
}
