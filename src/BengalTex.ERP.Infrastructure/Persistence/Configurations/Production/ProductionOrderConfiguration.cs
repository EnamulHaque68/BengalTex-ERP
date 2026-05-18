using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Production;

public class ProductionOrderConfiguration : IEntityTypeConfiguration<ProductionOrder>
{
    public void Configure(EntityTypeBuilder<ProductionOrder> builder)
    {
        builder.ToTable("ProductionOrders");

        builder.Property(p => p.Code).IsRequired().HasMaxLength(50);
        builder.Property(p => p.Quantity).HasPrecision(18, 4);
        builder.Property(p => p.CompletedBy).HasMaxLength(100);
        builder.Property(p => p.Notes).HasMaxLength(2000);

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(p => p.Code).IsUnique();
        builder.HasIndex(p => p.ProductId);
        builder.HasIndex(p => p.BomId);
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.PlannedStartDate);

        // Product FK — Restrict so a product with production orders can't be deleted
        builder.HasOne(p => p.Product)
            .WithMany()
            .HasForeignKey(p => p.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // BOM FK — Restrict so the snapshot version stays traceable
        builder.HasOne(p => p.Bom)
            .WithMany()
            .HasForeignKey(p => p.BomId)
            .OnDelete(DeleteBehavior.Restrict);

        // Issue warehouse FK — Restrict (required, history-preserving)
        builder.HasOne(p => p.IssueWarehouse)
            .WithMany()
            .HasForeignKey(p => p.IssueWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Receive warehouse FK — Restrict
        builder.HasOne(p => p.ReceiveWarehouse)
            .WithMany()
            .HasForeignKey(p => p.ReceiveWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(p => p.RowVersion).IsRowVersion();
    }
}
