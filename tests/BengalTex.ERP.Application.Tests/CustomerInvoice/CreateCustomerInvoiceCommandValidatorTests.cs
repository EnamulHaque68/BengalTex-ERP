using BengalTex.ERP.Application.CustomerInvoice.Commands;
using FluentAssertions;
using Xunit;

namespace BengalTex.ERP.Application.Tests.CustomerInvoice;

public class CreateCustomerInvoiceCommandValidatorTests
{
    private readonly CreateCustomerInvoiceCommandValidator _validator = new();

    private static CreateCustomerInvoiceCommand Valid(
        long salesOrderId = 1,
        decimal vatRate = 0.15m,
        IReadOnlyList<CustomerInvoiceLineInput>? lines = null) =>
        new(
            SalesOrderId: salesOrderId,
            VatRate: vatRate,
            InvoiceDate: new DateOnly(2026, 5, 22),
            DueDate: null,
            Notes: null,
            Lines: lines ?? new[] { new CustomerInvoiceLineInput(1, 10m, 5m, null) });

    [Fact]
    public void Valid_command_passes()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Missing_sales_order_fails()
    {
        var result = _validator.Validate(Valid(salesOrderId: 0));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCustomerInvoiceCommand.SalesOrderId));
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.5)]
    public void Vat_rate_out_of_range_fails(decimal vatRate)
    {
        var result = _validator.Validate(Valid(vatRate: vatRate));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateCustomerInvoiceCommand.VatRate));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.15)]
    [InlineData(1)]
    public void Vat_rate_in_range_passes(decimal vatRate)
    {
        _validator.Validate(Valid(vatRate: vatRate)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void No_lines_fails()
    {
        var result = _validator.Validate(Valid(lines: Array.Empty<CustomerInvoiceLineInput>()));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Duplicate_sales_order_line_fails()
    {
        // The same SO line may be billed at most once per invoice.
        var result = _validator.Validate(Valid(lines: new[]
        {
            new CustomerInvoiceLineInput(1, 10m, 5m, null, SalesOrderLineId: 7),
            new CustomerInvoiceLineInput(1, 1m, 2m, null, SalesOrderLineId: 7)
        }));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Same_product_from_two_different_so_lines_passes()
    {
        // A product may legitimately appear twice if it comes from two different SO lines.
        var result = _validator.Validate(Valid(lines: new[]
        {
            new CustomerInvoiceLineInput(1, 10m, 5m, null, SalesOrderLineId: 7),
            new CustomerInvoiceLineInput(1, 1m, 2m, null, SalesOrderLineId: 8)
        }));
        result.IsValid.Should().BeTrue();
    }
}
