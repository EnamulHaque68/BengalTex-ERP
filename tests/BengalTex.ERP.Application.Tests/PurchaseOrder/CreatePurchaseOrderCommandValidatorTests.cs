using BengalTex.ERP.Application.PurchaseOrder.Commands;
using FluentAssertions;
using Xunit;

namespace BengalTex.ERP.Application.Tests.PurchaseOrder;

public class CreatePurchaseOrderCommandValidatorTests
{
    private readonly CreatePurchaseOrderCommandValidator _validator = new();

    private static CreatePurchaseOrderCommand Valid(
        int supplierId = 1,
        int currencyId = 1,
        decimal exchangeRate = 1m,
        IReadOnlyList<PurchaseOrderLineInput>? lines = null) =>
        new(
            SupplierId: supplierId,
            OrderDate: new DateOnly(2026, 5, 22),
            ExpectedDeliveryDate: null,
            DeliveryWarehouseId: null,
            Notes: null,
            CurrencyId: currencyId,
            ExchangeRate: exchangeRate,
            Lines: lines ?? new[] { new PurchaseOrderLineInput(1, 10m, 5m, null) });

    [Fact]
    public void Valid_command_passes()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Missing_supplier_fails()
    {
        var result = _validator.Validate(Valid(supplierId: 0));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePurchaseOrderCommand.SupplierId));
    }

    [Fact]
    public void No_lines_fails()
    {
        var result = _validator.Validate(Valid(lines: Array.Empty<PurchaseOrderLineInput>()));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePurchaseOrderCommand.Lines));
    }

    [Fact]
    public void Duplicate_raw_material_fails()
    {
        var result = _validator.Validate(Valid(lines: new[]
        {
            new PurchaseOrderLineInput(1, 10m, 5m, null),
            new PurchaseOrderLineInput(1, 3m, 2m, null)   // same RM twice
        }));
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Non_positive_currency_fails(int currencyId)
    {
        var result = _validator.Validate(Valid(currencyId: currencyId));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePurchaseOrderCommand.CurrencyId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.5)]
    public void Non_positive_exchange_rate_fails(decimal rate)
    {
        var result = _validator.Validate(Valid(exchangeRate: rate));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePurchaseOrderCommand.ExchangeRate));
    }

    [Fact]
    public void Zero_quantity_line_fails()
    {
        var result = _validator.Validate(Valid(lines: new[] { new PurchaseOrderLineInput(1, 0m, 5m, null) }));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Negative_unit_price_fails()
    {
        var result = _validator.Validate(Valid(lines: new[] { new PurchaseOrderLineInput(1, 5m, -1m, null) }));
        result.IsValid.Should().BeFalse();
    }
}
