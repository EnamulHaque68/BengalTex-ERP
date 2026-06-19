using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Product;

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("ProductVariants");

        builder.Property(v => v.VariantCode).IsRequired().HasMaxLength(50);
        builder.Property(v => v.Name).HasMaxLength(200);
        builder.Property(v => v.Color).HasMaxLength(50);
        builder.Property(v => v.Size).HasMaxLength(50);
        builder.Property(v => v.Sku).HasMaxLength(100);
        builder.Property(v => v.Notes).HasMaxLength(1000);
        builder.Property(v => v.SalesPriceOverride).HasPrecision(18, 4);

        builder.HasIndex(v => v.ProductId);
        builder.HasIndex(v => v.Sku);

        // Variant code is unique within a product (ignoring soft-deleted rows)
        builder.HasIndex(v => new { v.ProductId, v.VariantCode })
            .HasFilter("[IsDeleted] = 0")
            .IsUnique()
            .HasDatabaseName("UX_ProductVariants_ProductCode");

        builder.HasOne(v => v.Product)
            .WithMany()
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(v => v.RowVersion).IsRowVersion();
    }
}
