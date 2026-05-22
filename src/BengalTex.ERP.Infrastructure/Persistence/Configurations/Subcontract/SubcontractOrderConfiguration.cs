using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Subcontract;

public class SubcontractOrderConfiguration : IEntityTypeConfiguration<SubcontractOrder>
{
    public void Configure(EntityTypeBuilder<SubcontractOrder> builder)
    {
        builder.ToTable("SubcontractOrders");

        builder.Property(s => s.Code).IsRequired().HasMaxLength(50);
        builder.Property(s => s.ProcessType).IsRequired().HasMaxLength(100);
        builder.Property(s => s.ChargeAmount).HasPrecision(18, 2);
        builder.Property(s => s.IssuedBy).HasMaxLength(100);
        builder.Property(s => s.ReceivedBy).HasMaxLength(100);
        builder.Property(s => s.Notes).HasMaxLength(2000);

        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(s => s.Code).IsUnique();
        builder.HasIndex(s => s.SubcontractorId);
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.OrderDate);

        builder.HasOne(s => s.Subcontractor)
            .WithMany()
            .HasForeignKey(s => s.SubcontractorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Warehouse)
            .WithMany()
            .HasForeignKey(s => s.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Lines)
            .WithOne(l => l.SubcontractOrder)
            .HasForeignKey(l => l.SubcontractOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(s => s.RowVersion).IsRowVersion();
    }
}

public class SubcontractLineConfiguration : IEntityTypeConfiguration<SubcontractLine>
{
    public void Configure(EntityTypeBuilder<SubcontractLine> builder)
    {
        builder.ToTable("SubcontractLines");

        builder.Property(l => l.IssuedQuantity).HasPrecision(18, 4);
        builder.Property(l => l.ReceivedQuantity).HasPrecision(18, 4);
        builder.Property(l => l.LineNotes).HasMaxLength(1000);

        // Exactly one of RawMaterialId / ProductId must be set
        builder.ToTable(t => t.HasCheckConstraint("CK_SubcontractLine_OneItemType",
            "([RawMaterialId] IS NOT NULL AND [ProductId] IS NULL) OR ([RawMaterialId] IS NULL AND [ProductId] IS NOT NULL)"));

        builder.HasIndex(l => l.SubcontractOrderId);
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
