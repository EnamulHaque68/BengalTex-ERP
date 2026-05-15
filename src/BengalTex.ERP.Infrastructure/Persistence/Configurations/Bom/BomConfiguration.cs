using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BomEntity = BengalTex.ERP.Domain.Entities.Bom;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Bom;

public class BomConfiguration : IEntityTypeConfiguration<BomEntity>
{
    public void Configure(EntityTypeBuilder<BomEntity> builder)
    {
        builder.ToTable("Boms");

        builder.Property(b => b.Code).IsRequired().HasMaxLength(50);
        builder.Property(b => b.Name).HasMaxLength(200);
        builder.Property(b => b.OutputQuantity).HasPrecision(18, 4);
        builder.Property(b => b.ApprovedBy).HasMaxLength(100);
        builder.Property(b => b.Notes).HasMaxLength(2000);

        builder.Property(b => b.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(b => b.Code).IsUnique();
        builder.HasIndex(b => b.Status);

        // Version is unique per product
        builder.HasIndex(b => new { b.ProductId, b.Version }).IsUnique();

        // At most one active BOM per product (filtered unique, ignores soft-deleted rows)
        builder.HasIndex(b => new { b.ProductId, b.IsActive })
            .HasFilter("[IsActive] = 1 AND [IsDeleted] = 0")
            .IsUnique()
            .HasDatabaseName("UX_Boms_OneActivePerProduct");

        // Product FK — Restrict so a product with BOMs can't be deleted out from under them
        builder.HasOne(b => b.Product)
            .WithMany()
            .HasForeignKey(b => b.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // One-to-many: Bom → BomLines
        builder.HasMany(b => b.Lines)
            .WithOne(l => l.Bom)
            .HasForeignKey(l => l.BomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(b => b.RowVersion).IsRowVersion();
    }
}

public class BomLineConfiguration : IEntityTypeConfiguration<BomLine>
{
    public void Configure(EntityTypeBuilder<BomLine> builder)
    {
        builder.ToTable("BomLines");

        builder.Property(l => l.Quantity).HasPrecision(18, 4);
        builder.Property(l => l.WastagePercent).HasPrecision(7, 4);
        builder.Property(l => l.LineNotes).HasMaxLength(1000);

        builder.HasIndex(l => l.BomId);
        builder.HasIndex(l => l.RawMaterialId);

        // RawMaterial FK — Restrict so a material referenced by a BOM line can't be deleted
        builder.HasOne(l => l.RawMaterial)
            .WithMany()
            .HasForeignKey(l => l.RawMaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(l => l.RowVersion).IsRowVersion();
    }
}
