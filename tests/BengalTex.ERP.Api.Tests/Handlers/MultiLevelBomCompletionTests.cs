using BengalTex.ERP.Api.Tests.TestSupport;
using BengalTex.ERP.Application.Accounting;
using BengalTex.ERP.Application.Production.Commands;
using BengalTex.ERP.Application.Production.Dtos;
using BengalTex.ERP.Application.Production.Queries;
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
/// Multi-level BOM completion: a BOM line can be a raw material OR a semi-finished component
/// product (sub-assembly). At Complete the RM is drawn via FIFO lots, the component product is
/// issued from stock, the material cost rolls up into the output product's WAC, and the WIP
/// backflush journal splits its credit between Raw Material Inventory (RM cost) and Finished
/// Goods Inventory (component cost). The pure-RM path must stay byte-for-byte unchanged.
/// </summary>
public class MultiLevelBomCompletionTests
{
    private readonly Mock<IStockService> _stock = new();
    private readonly Mock<IStockLotService> _lots = new();
    private readonly Mock<IJournalPostingService> _journal = new();
    private readonly List<(string Account, decimal Debit, decimal Credit)[]> _journalCalls = new();
    private readonly List<(int ProductId, decimal Qty)> _productMoves = new();
    private readonly List<(int RawMaterialId, decimal Qty)> _rmConsumed = new();

    public MultiLevelBomCompletionTests()
    {
        _stock.Setup(s => s.GetProductTotalOnHandAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(0m);
        _stock.Setup(s => s.GetRawMaterialOnHandAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(1000m);   // plenty of stock — no shortage
        _stock.Setup(s => s.GetProductOnHandAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(1000m);

        _stock.Setup(s => s.PostProductMovementAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<StockMovementType>(),
                It.IsAny<string?>(), It.IsAny<long?>(), It.IsAny<string?>(), It.IsAny<DateOnly>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>(), It.IsAny<StockLot?>()))
            .Callback<int, int, decimal, StockMovementType, string?, long?, string?, DateOnly, string?, CancellationToken, StockLot?>(
                (pid, _, qty, _, _, _, _, _, _, _, _) => _productMoves.Add((pid, qty)))
            .Returns(Task.CompletedTask);

        _lots.Setup(l => l.ConsumeRawMaterialFifoAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<decimal>(), It.IsAny<StockMovementType>(),
                It.IsAny<string?>(), It.IsAny<long?>(), It.IsAny<string?>(), It.IsAny<DateOnly>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<int, int, decimal, StockMovementType, string?, long?, string?, DateOnly, string?, CancellationToken>(
                (rmId, _, qty, _, _, _, _, _, _, _) => _rmConsumed.Add((rmId, qty)))
            .Returns(Task.CompletedTask);

        _journal.Setup(j => j.PostAsync(
                It.IsAny<DateOnly>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>(),
                It.IsAny<string>(), It.IsAny<IReadOnlyList<JournalPostingLine>>(), It.IsAny<CancellationToken>()))
            .Callback<DateOnly, string, string, long, string, IReadOnlyList<JournalPostingLine>, CancellationToken>(
                (_, _, _, _, _, lines, _) =>
                    _journalCalls.Add(lines.Select(l => (l.AccountCode, l.Debit, l.Credit)).ToArray()))
            .Returns(Task.CompletedTask);
    }

    private CompleteProductionOrderCommandHandler Handler(ApplicationDbContext ctx)
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetProductionOrderByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ApiResponse<ProductionOrderDto>.Ok(null!));
        return new CompleteProductionOrderCommandHandler(
            new Repository<ProductionOrder, long>(ctx),
            new UnitOfWork(ctx),
            _stock.Object, _lots.Object, new Mock<IStockReservationService>().Object, _journal.Object,
            new StubCurrentUser(), mediator.Object);
    }

    private static (string Account, decimal Debit, decimal Credit) Leg(
        (string, decimal, decimal)[] lines, string account)
        => lines.Single(l => l.Item1 == account);

    [Fact]
    public async Task Multilevel_bom_consumes_component_from_stock_and_splits_the_wip_journal()
    {
        await using var ctx = TestHarness.NewContext();
        // Output FG (id 1) made from 2× raw String (id 10, WAC 3) + 1× sub-assembly Printed-Tag (id 2, WAC 5)
        ctx.Products.Add(new Product { Id = 1, Code = "FG-1", Name = "Hangtag Set", UnitOfMeasureId = 1, WeightedAverageCost = 0m });
        ctx.Products.Add(new Product { Id = 2, Code = "FG-2", Name = "Printed Tag", UnitOfMeasureId = 1, WeightedAverageCost = 5m });
        ctx.RawMaterials.Add(new RawMaterial { Id = 10, Code = "RM-10", Name = "String", UnitOfMeasureId = 1, WeightedAverageCost = 3m, StandardCost = 3m });
        ctx.Boms.Add(new Bom
        {
            Id = 1, Code = "BOM-1", ProductId = 1, Version = 1, OutputQuantity = 1m,
            Lines =
            {
                new BomLine { RawMaterialId = 10, Quantity = 2m, WastagePercent = 0m, SortOrder = 0 },
                new BomLine { ComponentProductId = 2, Quantity = 1m, WastagePercent = 0m, SortOrder = 1 }
            }
        });
        ctx.ProductionOrders.Add(new ProductionOrder
        {
            Code = "PRD-1", ProductId = 1, BomId = 1, Quantity = 10m,
            IssueWarehouseId = 1, ReceiveWarehouseId = 1, Status = ProductionOrderStatus.InProgress
        });
        await ctx.SaveChangesAsync();

        var result = await Handler(ctx).Handle(new CompleteProductionOrderCommand(1), default);

        result.Success.Should().BeTrue();

        // rmCost = 2×10×3 = 60 ; componentCost = 1×10×5 = 50 ; total = 110
        _rmConsumed.Should().ContainSingle().Which.Should().Be((10, 20m));
        _productMoves.Should().Contain((2, -10m));   // sub-assembly issued OUT
        _productMoves.Should().Contain((1, 10m));    // finished good received IN

        _journalCalls.Should().HaveCount(2);
        var issue = _journalCalls[0];
        Leg(issue, LedgerAccounts.WorkInProgressInventory).Debit.Should().Be(110m);
        Leg(issue, LedgerAccounts.RawMaterialInventory).Credit.Should().Be(60m);
        Leg(issue, LedgerAccounts.FinishedGoodsInventory).Credit.Should().Be(50m);
        var receive = _journalCalls[1];
        Leg(receive, LedgerAccounts.FinishedGoodsInventory).Debit.Should().Be(110m);
        Leg(receive, LedgerAccounts.WorkInProgressInventory).Credit.Should().Be(110m);

        // Output WAC = total material cost ÷ produced qty = 110 ÷ 10 = 11 (was 0, no prior stock)
        ctx.Products.Single(p => p.Id == 1).WeightedAverageCost.Should().Be(11m);
    }

    [Fact]
    public async Task Pure_rm_bom_credits_only_raw_material_inventory_no_component_movement()
    {
        await using var ctx = TestHarness.NewContext();
        ctx.Products.Add(new Product { Id = 1, Code = "FG-1", Name = "Woven Label", UnitOfMeasureId = 1, WeightedAverageCost = 0m });
        ctx.RawMaterials.Add(new RawMaterial { Id = 10, Code = "RM-10", Name = "Taffeta", UnitOfMeasureId = 1, WeightedAverageCost = 4m, StandardCost = 4m });
        ctx.Boms.Add(new Bom
        {
            Id = 1, Code = "BOM-1", ProductId = 1, Version = 1, OutputQuantity = 1m,
            Lines = { new BomLine { RawMaterialId = 10, Quantity = 5m, WastagePercent = 0m, SortOrder = 0 } }
        });
        ctx.ProductionOrders.Add(new ProductionOrder
        {
            Code = "PRD-1", ProductId = 1, BomId = 1, Quantity = 10m,
            IssueWarehouseId = 1, ReceiveWarehouseId = 1, Status = ProductionOrderStatus.InProgress
        });
        await ctx.SaveChangesAsync();

        var result = await Handler(ctx).Handle(new CompleteProductionOrderCommand(1), default);

        result.Success.Should().BeTrue();

        // rmCost = 5×10×4 = 200 ; componentCost = 0
        _rmConsumed.Should().ContainSingle().Which.Should().Be((10, 50m));
        _productMoves.Should().ContainSingle().Which.Should().Be((1, 10m));   // only the FG receipt; no component issue

        var issue = _journalCalls[0];
        Leg(issue, LedgerAccounts.WorkInProgressInventory).Debit.Should().Be(200m);
        Leg(issue, LedgerAccounts.RawMaterialInventory).Credit.Should().Be(200m);
        // Backward-compatible: the FG credit leg is zero for a pure-RM BOM (real service drops it).
        Leg(issue, LedgerAccounts.FinishedGoodsInventory).Credit.Should().Be(0m);
    }
}
