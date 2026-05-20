using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.QuarantineDisposition;

public class QuarantineDispositionConfiguration : IEntityTypeConfiguration<Domain.Entities.QuarantineDisposition>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.QuarantineDisposition> builder)
    {
        builder.ToTable("QuarantineDispositions");

        builder.Property(d => d.Code).IsRequired().HasMaxLength(50);
        builder.Property(d => d.Reason).HasMaxLength(500);
        builder.Property(d => d.PostedBy).HasMaxLength(100);
        builder.Property(d => d.Notes).HasMaxLength(2000);

        builder.Property(d => d.DispositionType).HasConversion<string>().HasMaxLength(20);
        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(d => d.Code).IsUnique();
        builder.HasIndex(d => d.QuarantineWarehouseId);
        builder.HasIndex(d => d.DestinationWarehouseId);
        builder.HasIndex(d => d.Status);
        builder.HasIndex(d => d.DispositionDate);

        builder.HasOne(d => d.QuarantineWarehouse)
            .WithMany()
            .HasForeignKey(d => d.QuarantineWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.DestinationWarehouse)
            .WithMany()
            .HasForeignKey(d => d.DestinationWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(d => d.Lines)
            .WithOne(l => l.QuarantineDisposition)
            .HasForeignKey(l => l.QuarantineDispositionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(d => d.RowVersion).IsRowVersion();
    }
}

public class QuarantineDispositionLineConfiguration : IEntityTypeConfiguration<QuarantineDispositionLine>
{
    public void Configure(EntityTypeBuilder<QuarantineDispositionLine> builder)
    {
        builder.ToTable("QuarantineDispositionLines", t => t.HasCheckConstraint(
            "CK_QuarantineDispositionLine_OneItemType",
            "([RawMaterialId] IS NOT NULL AND [ProductId] IS NULL) OR ([RawMaterialId] IS NULL AND [ProductId] IS NOT NULL)"));

        builder.Property(l => l.Quantity).HasPrecision(18, 4);
        builder.Property(l => l.LineNotes).HasMaxLength(1000);

        builder.HasIndex(l => l.QuarantineDispositionId);
        builder.HasIndex(l => l.RawMaterialId);
        builder.HasIndex(l => l.ProductId);

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
