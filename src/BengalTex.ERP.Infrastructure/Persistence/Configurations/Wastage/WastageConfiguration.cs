using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Wastage;

public class WastageReasonConfiguration : IEntityTypeConfiguration<WastageReason>
{
    public void Configure(EntityTypeBuilder<WastageReason> builder)
    {
        builder.ToTable("WastageReasons");
        builder.Property(r => r.Name).IsRequired().HasMaxLength(150);
        builder.Property(r => r.Description).HasMaxLength(500);
        builder.HasIndex(r => r.Name);
        builder.Property(r => r.RowVersion).IsRowVersion();
    }
}

public class WastageEntryConfiguration : IEntityTypeConfiguration<WastageEntry>
{
    public void Configure(EntityTypeBuilder<WastageEntry> builder)
    {
        builder.ToTable("WastageEntries");

        builder.Property(w => w.Code).IsRequired().HasMaxLength(50);
        builder.Property(w => w.Department).HasMaxLength(150);
        builder.Property(w => w.Notes).HasMaxLength(1000);
        builder.Property(w => w.Quantity).HasPrecision(18, 4);
        builder.Property(w => w.UnitCost).HasPrecision(18, 4);
        builder.Property(w => w.TotalCost).HasPrecision(18, 2);

        builder.HasIndex(w => w.Code).IsUnique();
        builder.HasIndex(w => w.WastageDate);
        builder.HasIndex(w => w.RawMaterialId);
        builder.HasIndex(w => w.WastageReasonId);
        builder.HasIndex(w => w.ProductionOrderId);

        builder.HasOne(w => w.RawMaterial)
            .WithMany().HasForeignKey(w => w.RawMaterialId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(w => w.WastageReason)
            .WithMany().HasForeignKey(w => w.WastageReasonId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(w => w.ProductionOrder)
            .WithMany().HasForeignKey(w => w.ProductionOrderId).OnDelete(DeleteBehavior.SetNull);

        builder.Property(w => w.RowVersion).IsRowVersion();
    }
}
