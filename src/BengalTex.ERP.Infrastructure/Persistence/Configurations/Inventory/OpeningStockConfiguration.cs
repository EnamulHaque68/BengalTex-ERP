using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Inventory;

public class OpeningStockEntryConfiguration : IEntityTypeConfiguration<OpeningStockEntry>
{
    public void Configure(EntityTypeBuilder<OpeningStockEntry> builder)
    {
        builder.ToTable("OpeningStockEntries");

        builder.Property(e => e.Code).IsRequired().HasMaxLength(50);
        builder.Property(e => e.PostedBy).HasMaxLength(100);
        builder.Property(e => e.Notes).HasMaxLength(2000);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(e => e.Code).IsUnique();
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.EntryDate);

        builder.HasOne(e => e.Warehouse)
            .WithMany()
            .HasForeignKey(e => e.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Lines)
            .WithOne(l => l.OpeningStockEntry)
            .HasForeignKey(l => l.OpeningStockEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(e => e.RowVersion).IsRowVersion();
    }
}

public class OpeningStockLineConfiguration : IEntityTypeConfiguration<OpeningStockLine>
{
    public void Configure(EntityTypeBuilder<OpeningStockLine> builder)
    {
        // Polymorphic item — exactly one of RawMaterialId / ProductId is set.
        builder.ToTable("OpeningStockLines", t => t.HasCheckConstraint(
            "CK_OpeningStockLine_OneItem",
            "([RawMaterialId] IS NOT NULL AND [ProductId] IS NULL) OR ([RawMaterialId] IS NULL AND [ProductId] IS NOT NULL)"));

        builder.Property(l => l.Quantity).HasPrecision(18, 4);
        builder.Property(l => l.UnitCost).HasPrecision(18, 2);
        builder.Property(l => l.LineNotes).HasMaxLength(1000);

        builder.HasIndex(l => l.OpeningStockEntryId);

        builder.HasOne(l => l.RawMaterial)
            .WithMany()
            .HasForeignKey(l => l.RawMaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Product)
            .WithMany()
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(l => l.RowVersion).IsRowVersion();
    }
}
