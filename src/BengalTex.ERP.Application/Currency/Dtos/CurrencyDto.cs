namespace BengalTex.ERP.Application.Currency.Dtos;

public record CurrencyDto(
    int Id,
    string Code,
    string Name,
    string Symbol,
    decimal ExchangeRateToBase,
    bool IsBaseCurrency,
    bool IsActive);
