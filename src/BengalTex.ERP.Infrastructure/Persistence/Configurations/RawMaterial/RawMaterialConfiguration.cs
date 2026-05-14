using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RawMaterialEntity = BengalTex.ERP.Domain.Entities.RawMaterial;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.RawMaterial;

public class RawMaterialConfiguration : IEntityTypeConfiguration<RawMaterialEntity>
{
    public void Configure(EntityTypeBuilder<RawMaterialEntity> builder)
    {
        builder.ToTable("RawMaterials");

        builder.Property(r => r.Code).IsRequired().HasMaxLength(50);
        builder.Property(r => r.Name).IsRequired().HasMaxLength(200);
        builder.Property(r => r.Specification).HasMaxLength(1000);

        builder.Property(r => r.Category)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(r => r.MinimumStockLevel).HasPrecision(18, 4);
        builder.Property(r => r.OpeningStock).HasPrecision(18, 4);
        builder.Property(r => r.StandardCost).HasPrecision(18, 4);

        builder.Property(r => r.Notes).HasMaxLength(2000);

        builder.HasIndex(r => r.Code).IsUnique();
        builder.HasIndex(r => r.Name);
        builder.HasIndex(r => r.Category);
        builder.HasIndex(r => r.IsActive);

        // UoM FK — Restrict so a unit in use can't be deleted out from under raw materials
        builder.HasOne(r => r.UnitOfMeasure)
            .WithMany()
            .HasForeignKey(r => r.UnitOfMeasureId)
            .OnDelete(DeleteBehavior.Restrict);

        // Optional preferred supplier — SetNull so deleting a supplier just clears the link
        builder.HasOne(r => r.PreferredSupplier)
            .WithMany()
            .HasForeignKey(r => r.PreferredSupplierId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(r => r.RowVersion).IsRowVersion();
    }
}
