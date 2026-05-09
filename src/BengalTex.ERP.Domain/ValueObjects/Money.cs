namespace BengalTex.ERP.Domain.ValueObjects;

/// <summary>
/// Money value object. Always carries currency, original amount, and base-currency equivalent.
/// Base currency is BDT. Exchange rate is captured at transaction time.
/// </summary>
public sealed record Money
{
    public const string BaseCurrencyCode = "BDT";

    public decimal Amount { get; init; }
    public string CurrencyCode { get; init; }
    public decimal ExchangeRateToBase { get; init; }
    public decimal BaseAmount => Math.Round(Amount * ExchangeRateToBase, 4, MidpointRounding.AwayFromZero);

    private Money(decimal amount, string currencyCode, decimal exchangeRateToBase)
    {
        Amount = amount;
        CurrencyCode = currencyCode;
        ExchangeRateToBase = exchangeRateToBase;
    }

    public static Money Create(decimal amount, string currencyCode, decimal exchangeRateToBase)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
            throw new ArgumentException("Currency code is required.", nameof(currencyCode));
        if (currencyCode.Length != 3)
            throw new ArgumentException("Currency code must be 3 letters (ISO 4217).", nameof(currencyCode));
        if (exchangeRateToBase <= 0)
            throw new ArgumentException("Exchange rate must be positive.", nameof(exchangeRateToBase));

        return new Money(amount, currencyCode.ToUpperInvariant(), exchangeRateToBase);
    }

    public static Money InBase(decimal amount) =>
        new(amount, BaseCurrencyCode, 1m);

    public static Money Zero(string currencyCode = BaseCurrencyCode) =>
        new(0m, currencyCode.ToUpperInvariant(), 1m);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, CurrencyCode, ExchangeRateToBase);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, CurrencyCode, ExchangeRateToBase);
    }

    public Money Multiply(decimal factor) =>
        new(Amount * factor, CurrencyCode, ExchangeRateToBase);

    private void EnsureSameCurrency(Money other)
    {
        if (CurrencyCode != other.CurrencyCode)
            throw new InvalidOperationException(
                $"Cannot operate on different currencies: {CurrencyCode} vs {other.CurrencyCode}.");
    }

    public override string ToString() =>
        $"{Amount:N4} {CurrencyCode} (≈ {BaseAmount:N4} {BaseCurrencyCode})";
}