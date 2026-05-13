using BengalTex.ERP.Application.Customer.Dtos;
using Mapster;

namespace BengalTex.ERP.Application.Customer;

public class CustomerMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Entity → full DTO: enum needs ToString()
        config.NewConfig<Domain.Entities.Customer, CustomerDto>()
            .Map(d => d.Category, s => s.Category.ToString());

        // Entity → list-item DTO
        config.NewConfig<Domain.Entities.Customer, CustomerListItemDto>()
            .Map(d => d.Category, s => s.Category.ToString());
    }
}
