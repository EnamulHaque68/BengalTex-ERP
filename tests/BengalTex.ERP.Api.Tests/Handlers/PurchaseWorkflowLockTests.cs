using BengalTex.ERP.Api.Tests.TestSupport;
using BengalTex.ERP.Application.PurchaseRequisitions.Commands;
using BengalTex.ERP.Application.SupplierQuotations;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Infrastructure.Persistence;
using BengalTex.ERP.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace BengalTex.ERP.Api.Tests.Handlers;

/// <summary>
/// Purchase workflow lock (Direct vs RFQ mutual exclusivity + duplicate-PO prevention). The PO
/// creation itself is delegated to a mediator (mocked); these guards all fire before that.
/// </summary>
public class PurchaseWorkflowLockTests
{
    private static async Task<long> SeedApprovedPr(ApplicationDbContext ctx, PurchaseRequisitionStatus status = PurchaseRequisitionStatus.Approved)
    {
        ctx.RawMaterials.Add(new RawMaterial { Id = 1, Code = "RM-1", Name = "Cotton", UnitOfMeasureId = 1 });
        var pr = new PurchaseRequisition
        {
            Code = "PR-1", RequisitionDate = new DateOnly(2026, 6, 1), Status = status,
            Lines = { new PurchaseRequisitionLine { RawMaterialId = 1, Quantity = 100m, EstimatedUnitPrice = 10m } }
        };
        ctx.PurchaseRequisitions.Add(pr);
        await ctx.SaveChangesAsync();
        return pr.Id;
    }

    private static SupplierQuotation Sq(long prId, SupplierQuotationStatus status) => new()
    {
        Code = "SQ-" + Guid.NewGuid().ToString("N")[..4], QuotationDate = new DateOnly(2026, 6, 2),
        SupplierId = 1, PurchaseRequisitionId = prId, CurrencyId = 1, ExchangeRate = 1m, Status = status,
        Lines = { new SupplierQuotationLine { RawMaterialId = 1, Quantity = 100m, UnitPrice = 9m } }
    };

    [Fact]
    public async Task Direct_convert_is_blocked_once_an_rfq_is_started()
    {
        await using var ctx = TestHarness.NewContext();
        var prId = await SeedApprovedPr(ctx);
        ctx.SupplierQuotations.Add(Sq(prId, SupplierQuotationStatus.Submitted));   // RFQ started
        await ctx.SaveChangesAsync();

        var mediator = new Mock<IMediator>();
        var handler = new ConvertPurchaseRequisitionToPoCommandHandler(
            new Repository<PurchaseRequisition, long>(ctx),
            new Repository<SupplierQuotation, long>(ctx),
            new UnitOfWork(ctx), mediator.Object);

        var res = await handler.Handle(new ConvertPurchaseRequisitionToPoCommand(
            prId, 1, new DateOnly(2026, 6, 3), null, null, 1, 1m, null,
            Array.Empty<ConvertPrLinePriceInput>()), default);

        res.Success.Should().BeFalse();
        res.Message.Should().Contain("RFQ");
    }

    [Fact]
    public async Task Rfq_is_blocked_on_a_directly_converted_requisition()
    {
        await using var ctx = TestHarness.NewContext();
        var prId = await SeedApprovedPr(ctx, PurchaseRequisitionStatus.Converted);
        ctx.Suppliers.Add(new Supplier { Id = 1, Code = "S-1", Name = "Mills" });
        ctx.Currencies.Add(new Currency { Id = 1, Code = "BDT", Name = "Taka", Symbol = "BDT", ExchangeRateToBase = 1m });
        await ctx.SaveChangesAsync();

        var handler = new CreateSupplierQuotationCommandHandler(
            new Repository<SupplierQuotation, long>(ctx),
            new Repository<Supplier>(ctx), new Repository<Currency>(ctx), new Repository<RawMaterial>(ctx),
            new Repository<PurchaseRequisition, long>(ctx),
            new UnitOfWork(ctx), TestHarness.Numbering().Object);

        var res = await handler.Handle(new CreateSupplierQuotationCommand(
            new DateOnly(2026, 6, 2), 1, prId, 1, 1m, null, null,
            new[] { new SupplierQuotationLineInput(1, 100m, 9m, null, null) }), default);

        res.Success.Should().BeFalse();
        res.Message.Should().Contain("converted directly");
    }

    [Fact]
    public async Task Selecting_a_second_quotation_for_the_same_requisition_is_blocked()
    {
        await using var ctx = TestHarness.NewContext();
        var prId = await SeedApprovedPr(ctx);
        ctx.SupplierQuotations.Add(Sq(prId, SupplierQuotationStatus.Selected));    // winner already chosen
        var second = Sq(prId, SupplierQuotationStatus.Submitted);
        ctx.SupplierQuotations.Add(second);
        await ctx.SaveChangesAsync();

        var handler = new SelectSupplierQuotationCommandHandler(
            new Repository<SupplierQuotation, long>(ctx), new StubCurrentUser(),
            new UnitOfWork(ctx), new Mock<IMediator>().Object);

        var res = await handler.Handle(new SelectSupplierQuotationCommand(second.Id), default);

        res.Success.Should().BeFalse();
        res.Message.Should().Contain("already been selected");
    }
}
