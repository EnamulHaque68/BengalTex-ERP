using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductEntity = BengalTex.ERP.Domain.Entities.Product;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Product;

public class ProductConfiguration : IEntityTypeConfiguration<ProductEntity>
{
    public void Configure(EntityTypeBuilder<ProductEntity> builder)
    {
        builder.ToTable("Products");

        builder.Property(p => p.Code).IsRequired().HasMaxLength(50);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Specification).HasMaxLength(1000);

        builder.Property(p => p.Size).HasMaxLength(50);
        builder.Property(p => p.Color).HasMaxLength(50);
        builder.Property(p => p.Material).HasMaxLength(100);

        builder.Property(p => p.SalesPrice).HasPrecision(18, 4);
        builder.Property(p => p.ReorderLevel).HasPrecision(18, 4);
        builder.Property(p => p.WeightedAverageCost).HasPrecision(18, 4);

        builder.Property(p => p.ImageUrl).HasMaxLength(500);
        builder.Property(p => p.Notes).HasMaxLength(2000);

        builder.HasIndex(p => p.Code).IsUnique();
        builder.HasIndex(p => p.Name);
        builder.HasIndex(p => p.ProductCategoryId);
        builder.HasIndex(p => p.IsActive);

        // UnitOfMeasure FK — Restrict so a unit in use can't be deleted out from under products
        builder.HasOne(p => p.UnitOfMeasure)
            .WithMany()
            .HasForeignKey(p => p.UnitOfMeasureId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(p => p.RowVersion).IsRowVersion();
    }
}
