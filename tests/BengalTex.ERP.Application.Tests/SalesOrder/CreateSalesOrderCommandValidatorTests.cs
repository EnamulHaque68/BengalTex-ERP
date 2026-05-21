using BengalTex.ERP.Application.SalesOrder.Commands;
using FluentAssertions;
using Xunit;

namespace BengalTex.ERP.Application.Tests.SalesOrder;

public class CreateSalesOrderCommandValidatorTests
{
    private readonly CreateSalesOrderCommandValidator _validator = new();

    private static CreateSalesOrderCommand Valid(
        int customerId = 1,
        int currencyId = 1,
        decimal exchangeRate = 1m,
        IReadOnlyList<SalesOrderLineInput>? lines = null) =>
        new(
            CustomerId: customerId,
            OrderDate: new DateOnly(2026, 5, 22),
            RequiredDeliveryDate: null,
            CustomerPoRef: null,
            DeliveryAddress: null,
            Notes: null,
            CurrencyId: currencyId,
            ExchangeRate: exchangeRate,
            Lines: lines ?? new[] { new SalesOrderLineInput(1, 10m, 5m, null) });

    [Fact]
    public void Valid_command_passes()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Missing_customer_fails()
    {
        var result = _validator.Validate(Valid(customerId: 0));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSalesOrderCommand.CustomerId));
    }

    [Fact]
    public void No_lines_fails()
    {
        var result = _validator.Validate(Valid(lines: Array.Empty<SalesOrderLineInput>()));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Duplicate_product_fails()
    {
        var result = _validator.Validate(Valid(lines: new[]
        {
            new SalesOrderLineInput(1, 10m, 5m, null),
            new SalesOrderLineInput(1, 2m, 3m, null)
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
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSalesOrderCommand.CurrencyId));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void Non_positive_exchange_rate_fails(decimal rate)
    {
        var result = _validator.Validate(Valid(exchangeRate: rate));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateSalesOrderCommand.ExchangeRate));
    }
}
