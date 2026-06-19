using BengalTex.ERP.Api.Tests.TestSupport;
using BengalTex.ERP.Application.SupplierQuotations;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Infrastructure.Persistence;
using BengalTex.ERP.Infrastructure.Persistence.Repositories;
using FluentAssertions;
using Xunit;

namespace BengalTex.ERP.Api.Tests.Handlers;

/// <summary>
/// The RFQ comparison matrix: rows = the requisition's materials (at PR quantity), columns =
/// competing Submitted/Selected quotes. Each cell's unit price is converted to base currency
/// (× the quote's exchange rate); the lowest price per row and the lowest supplier total are flagged.
/// </summary>
public class QuotationComparisonTests
{
    private static GetQuotationComparisonQueryHandler Handler(ApplicationDbContext ctx) =>
        new(new Repository<SupplierQuotation, long>(ctx), new Repository<PurchaseRequisition, long>(ctx));

    [Fact]
    public async Task Flags_lowest_price_per_row_and_lowest_total_with_base_conversion()
    {
        await using var ctx = TestHarness.NewContext();
        ctx.RawMaterials.Add(new RawMaterial { Id = 10, Code = "RM-10", Name = "Yarn", UnitOfMeasureId = 1 });
        ctx.RawMaterials.Add(new RawMaterial { Id = 20, Code = "RM-20", Name = "Dye", UnitOfMeasureId = 1 });
        ctx.Currencies.Add(new Currency { Id = 1, Code = "BDT", Name = "Taka", ExchangeRateToBase = 1m, IsBaseCurrency = true });
        ctx.Currencies.Add(new Currency { Id = 2, Code = "USD", Name = "Dollar", ExchangeRateToBase = 120m });
        ctx.Suppliers.Add(new Supplier { Id = 1, Code = "SUP-1", Name = "Alpha" });
        ctx.Suppliers.Add(new Supplier { Id = 2, Code = "SUP-2", Name = "Beta" });

        ctx.PurchaseRequisitions.Add(new PurchaseRequisition
        {
            Id = 1, Code = "PR-1", RequisitionDate = new DateOnly(2026, 6, 1),
            Lines =
            {
                new PurchaseRequisitionLine { RawMaterialId = 10, Quantity = 100m, SortOrder = 0 },
                new PurchaseRequisitionLine { RawMaterialId = 20, Quantity = 50m, SortOrder = 1 }
            }
        });

        // Quote A — currency rate 2 (so base = price × 2): RM10 @ 2.5 (base 5), RM20 @ 6 (base 12)
        ctx.SupplierQuotations.Add(new SupplierQuotation
        {
            Id = 1, Code = "SQ-1", QuotationDate = new DateOnly(2026, 6, 2), SupplierId = 1,
            PurchaseRequisitionId = 1, CurrencyId = 2, ExchangeRate = 2m, Status = SupplierQuotationStatus.Submitted,
            Lines =
            {
                new SupplierQuotationLine { RawMaterialId = 10, Quantity = 100m, UnitPrice = 2.5m, SortOrder = 0 },
                new SupplierQuotationLine { RawMaterialId = 20, Quantity = 50m, UnitPrice = 6m, SortOrder = 1 }
            }
        });
        // Quote B — BDT rate 1: RM10 @ 6, RM20 @ 11
        ctx.SupplierQuotations.Add(new SupplierQuotation
        {
            Id = 2, Code = "SQ-2", QuotationDate = new DateOnly(2026, 6, 2), SupplierId = 2,
            PurchaseRequisitionId = 1, CurrencyId = 1, ExchangeRate = 1m, Status = SupplierQuotationStatus.Submitted,
            Lines =
            {
                new SupplierQuotationLine { RawMaterialId = 10, Quantity = 100m, UnitPrice = 6m, SortOrder = 0 },
                new SupplierQuotationLine { RawMaterialId = 20, Quantity = 50m, UnitPrice = 11m, SortOrder = 1 }
            }
        });
        await ctx.SaveChangesAsync();

        var result = await Handler(ctx).Handle(new GetQuotationComparisonQuery(1), default);

        result.Success.Should().BeTrue();
        var dto = result.Data!;
        dto.Suppliers.Should().HaveCount(2);
        dto.Rows.Should().HaveCount(2);

        // Row RM-10: A base 5 < B base 6 → A lowest
        var rm10 = dto.Rows.Single(r => r.RawMaterialId == 10);
        rm10.Cells.Single(c => c.SupplierQuotationId == 1).UnitPriceBase.Should().Be(5m);
        rm10.Cells.Single(c => c.SupplierQuotationId == 1).IsLowest.Should().BeTrue();
        rm10.Cells.Single(c => c.SupplierQuotationId == 2).IsLowest.Should().BeFalse();

        // Row RM-20: A base 12 > B base 11 → B lowest
        var rm20 = dto.Rows.Single(r => r.RawMaterialId == 20);
        rm20.Cells.Single(c => c.SupplierQuotationId == 2).IsLowest.Should().BeTrue();

        // Totals at PR qty: A = 100×5 + 50×12 = 1100 (lowest); B = 100×6 + 50×11 = 1150
        dto.Suppliers.Single(s => s.SupplierQuotationId == 1).TotalBase.Should().Be(1100m);
        dto.Suppliers.Single(s => s.SupplierQuotationId == 1).IsLowestTotal.Should().BeTrue();
        dto.Suppliers.Single(s => s.SupplierQuotationId == 2).TotalBase.Should().Be(1150m);
        dto.Suppliers.Single(s => s.SupplierQuotationId == 2).IsLowestTotal.Should().BeFalse();
    }

    [Fact]
    public async Task Draft_quotes_are_excluded_from_comparison()
    {
        await using var ctx = TestHarness.NewContext();
        ctx.RawMaterials.Add(new RawMaterial { Id = 10, Code = "RM-10", Name = "Yarn", UnitOfMeasureId = 1 });
        ctx.Currencies.Add(new Currency { Id = 1, Code = "BDT", Name = "Taka", ExchangeRateToBase = 1m, IsBaseCurrency = true });
        ctx.Suppliers.Add(new Supplier { Id = 1, Code = "SUP-1", Name = "Alpha" });
        ctx.PurchaseRequisitions.Add(new PurchaseRequisition
        {
            Id = 1, Code = "PR-1", RequisitionDate = new DateOnly(2026, 6, 1),
            Lines = { new PurchaseRequisitionLine { RawMaterialId = 10, Quantity = 100m, SortOrder = 0 } }
        });
        ctx.SupplierQuotations.Add(new SupplierQuotation
        {
            Id = 1, Code = "SQ-1", QuotationDate = new DateOnly(2026, 6, 2), SupplierId = 1,
            PurchaseRequisitionId = 1, CurrencyId = 1, ExchangeRate = 1m, Status = SupplierQuotationStatus.Draft,
            Lines = { new SupplierQuotationLine { RawMaterialId = 10, Quantity = 100m, UnitPrice = 5m, SortOrder = 0 } }
        });
        await ctx.SaveChangesAsync();

        var result = await Handler(ctx).Handle(new GetQuotationComparisonQuery(1), default);

        result.Success.Should().BeTrue();
        result.Data!.Suppliers.Should().BeEmpty();   // draft excluded
    }
}
