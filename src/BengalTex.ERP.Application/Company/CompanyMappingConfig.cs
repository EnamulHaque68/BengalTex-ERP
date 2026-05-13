using BengalTex.ERP.Application.Company.Dtos;
using Mapster;

namespace BengalTex.ERP.Application.Company;

/// <summary>
/// Entity → DTO mappings for Company. Auto-discovered by Mapster.Scan(assembly) in DI.
/// Command → Entity mappings are intentionally NOT here — handlers assign explicitly
/// to protect audit fields (CreatedAt, ModifiedAt, IsDeleted, RowVersion).
/// </summary>
public class CompanyMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Domain.Entities.Company, CompanyDto>();
    }
}
