using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PurchaseOrderEntity = BengalTex.ERP.Domain.Entities.PurchaseOrder;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.PurchaseOrder;

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrderEntity>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderEntity> builder)
    {
        builder.ToTable("PurchaseOrders");

        builder.Property(p => p.Code).IsRequired().HasMaxLength(50);
        builder.Property(p => p.ApprovedBy).HasMaxLength(100);
        builder.Property(p => p.Notes).HasMaxLength(2000);

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(p => p.Code).IsUnique();
        builder.HasIndex(p => p.SupplierId);
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.OrderDate);

        // Supplier FK — Restrict so a supplier with POs can't be deleted out from under them
        builder.HasOne(p => p.Supplier)
            .WithMany()
            .HasForeignKey(p => p.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        // Optional delivery warehouse — SetNull so deleting a warehouse just clears the link
        builder.HasOne(p => p.DeliveryWarehouse)
            .WithMany()
            .HasForeignKey(p => p.DeliveryWarehouseId)
            .OnDelete(DeleteBehavior.SetNull);

        // Transaction currency (Phase 21)
        builder.Property(p => p.ExchangeRate).HasPrecision(18, 6);
        builder.HasOne(p => p.Currency)
            .WithMany()
            .HasForeignKey(p => p.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        // One-to-many: PurchaseOrder → PurchaseOrderLines
        builder.HasMany(p => p.Lines)
            .WithOne(l => l.PurchaseOrder)
            .HasForeignKey(l => l.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(p => p.RowVersion).IsRowVersion();
    }
}

public class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLine> builder)
    {
        builder.ToTable("PurchaseOrderLines");

        builder.Property(l => l.Quantity).HasPrecision(18, 4);
        builder.Property(l => l.UnitPrice).HasPrecision(18, 4);
        builder.Property(l => l.ReceivedQuantity).HasPrecision(18, 4);
        builder.Property(l => l.LineNotes).HasMaxLength(1000);

        builder.HasIndex(l => l.PurchaseOrderId);
        builder.HasIndex(l => l.RawMaterialId);

        // RawMaterial FK — Restrict so a material referenced by a PO line can't be deleted
        builder.HasOne(l => l.RawMaterial)
            .WithMany()
            .HasForeignKey(l => l.RawMaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(l => l.RowVersion).IsRowVersion();
    }
}
