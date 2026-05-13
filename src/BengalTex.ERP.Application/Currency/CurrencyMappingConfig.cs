using BengalTex.ERP.Application.Currency.Dtos;
using Mapster;

namespace BengalTex.ERP.Application.Currency;

public class CurrencyMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Domain.Entities.Currency, CurrencyDto>();
    }
}
