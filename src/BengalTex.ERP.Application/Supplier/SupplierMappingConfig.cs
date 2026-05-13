using BengalTex.ERP.Application.Supplier.Dtos;
using Mapster;

namespace BengalTex.ERP.Application.Supplier;

public class SupplierMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Domain.Entities.Supplier, SupplierDto>();
        config.NewConfig<Domain.Entities.Supplier, SupplierListItemDto>();
    }
}
