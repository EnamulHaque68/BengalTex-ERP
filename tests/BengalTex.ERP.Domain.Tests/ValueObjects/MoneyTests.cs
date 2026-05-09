using BengalTex.ERP.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace BengalTex.ERP.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Create_WithValidInputs_BuildsMoney()
    {
        var m = Money.Create(100m, "usd", 110.5m);
        m.Amount.Should().Be(100m);
        m.CurrencyCode.Should().Be("USD"); // Normalized to uppercase
        m.ExchangeRateToBase.Should().Be(110.5m);
        m.BaseAmount.Should().Be(11050m);
    }

    [Fact]
    public void InBase_ReturnsBdtWithRateOne()
    {
        var m = Money.InBase(500m);
        m.CurrencyCode.Should().Be("BDT");
        m.ExchangeRateToBase.Should().Be(1m);
        m.BaseAmount.Should().Be(500m);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("US")]
    [InlineData("USDX")]
    public void Create_WithInvalidCurrency_Throws(string code)
    {
        var act = () => Money.Create(100m, code, 1m);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_WithNonPositiveRate_Throws(decimal rate)
    {
        var act = () => Money.Create(100m, "USD", rate);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Add_SameCurrency_AddsAmounts()
    {
        var a = Money.Create(100m, "USD", 110m);
        var b = Money.Create(50m, "USD", 110m);
        var sum = a.Add(b);
        sum.Amount.Should().Be(150m);
        sum.CurrencyCode.Should().Be("USD");
    }

    [Fact]
    public void Add_DifferentCurrencies_Throws()
    {
        var a = Money.Create(100m, "USD", 110m);
        var b = Money.Create(50m, "EUR", 120m);
        var act = () => a.Add(b);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Multiply_ScalesAmount()
    {
        var a = Money.Create(100m, "USD", 110m);
        var doubled = a.Multiply(2m);
        doubled.Amount.Should().Be(200m);
        doubled.BaseAmount.Should().Be(22000m);
    }

    [Fact]
    public void Records_AreEqualByValue()
    {
        var a = Money.Create(100m, "USD", 110m);
        var b = Money.Create(100m, "USD", 110m);
        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }
}