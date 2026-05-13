using BengalTex.ERP.Application.Factory.Dtos;
using Mapster;

namespace BengalTex.ERP.Application.Factory;

/// <summary>
/// Entity → DTO mappings for Factory. Auto-discovered by Mapster.Scan(assembly) in DI.
/// Command → Entity mappings are intentionally NOT here — handlers assign explicitly
/// to protect audit fields and apply business rules (e.g., Code.ToUpperInvariant()).
/// </summary>
public class FactoryMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Domain.Entities.Factory, FactoryDto>();

        config.NewConfig<Domain.Entities.Factory, FactoryListItemDto>()
            .Map(dst => dst.HasGeoFence,
                 src => src.GeoFenceLat != null && src.GeoFenceLng != null);
    }
}
