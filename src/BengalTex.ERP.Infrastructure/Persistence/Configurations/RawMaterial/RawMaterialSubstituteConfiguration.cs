using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.RawMaterial;

public class RawMaterialSubstituteConfiguration : IEntityTypeConfiguration<RawMaterialSubstitute>
{
    public void Configure(EntityTypeBuilder<RawMaterialSubstitute> builder)
    {
        builder.ToTable("RawMaterialSubstitutes");

        builder.Property(s => s.ConversionFactor).HasPrecision(18, 4);
        builder.Property(s => s.Notes).HasMaxLength(500);

        builder.HasIndex(s => s.RawMaterialId);
        builder.HasIndex(s => s.SubstituteRawMaterialId);

        // One substitute row per (primary, substitute) pair (ignoring soft-deleted rows)
        builder.HasIndex(s => new { s.RawMaterialId, s.SubstituteRawMaterialId })
            .HasFilter("[IsDeleted] = 0")
            .IsUnique()
            .HasDatabaseName("UX_RawMaterialSubstitutes_Pair");

        // Two FKs to RawMaterials — both Restrict to avoid multiple-cascade-path issues
        // (raw materials are soft-deleted, so cascade cleanup isn't needed).
        builder.HasOne(s => s.RawMaterial)
            .WithMany()
            .HasForeignKey(s => s.RawMaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.SubstituteRawMaterial)
            .WithMany()
            .HasForeignKey(s => s.SubstituteRawMaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(s => s.RowVersion).IsRowVersion();
    }
}
