using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Inventory;

public class StockOnHandConfiguration : IEntityTypeConfiguration<StockOnHand>
{
    public void Configure(EntityTypeBuilder<StockOnHand> builder)
    {
        builder.ToTable("StockOnHand", t => t.HasCheckConstraint(
            "CK_StockOnHand_OneItemType",
            "([RawMaterialId] IS NOT NULL AND [ProductId] IS NULL) OR ([RawMaterialId] IS NULL AND [ProductId] IS NOT NULL)"));

        builder.Property(s => s.Quantity).HasPrecision(18, 4);

        // Two filtered unique indexes — exactly one snapshot row per (RM, Warehouse) and per (Product, Warehouse)
        builder.HasIndex(s => new { s.RawMaterialId, s.WarehouseId })
            .IsUnique()
            .HasFilter("[RawMaterialId] IS NOT NULL AND [IsDeleted] = 0")
            .HasDatabaseName("UX_StockOnHand_RawMaterialWarehouse");

        builder.HasIndex(s => new { s.ProductId, s.WarehouseId })
            .IsUnique()
            .HasFilter("[ProductId] IS NOT NULL AND [IsDeleted] = 0")
            .HasDatabaseName("UX_StockOnHand_ProductWarehouse");

        builder.HasOne(s => s.RawMaterial)
            .WithMany()
            .HasForeignKey(s => s.RawMaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Product)
            .WithMany()
            .HasForeignKey(s => s.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Warehouse)
            .WithMany()
            .HasForeignKey(s => s.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(s => s.RowVersion).IsRowVersion();
    }
}

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements", t => t.HasCheckConstraint(
            "CK_StockMovement_OneItemType",
            "([RawMaterialId] IS NOT NULL AND [ProductId] IS NULL) OR ([RawMaterialId] IS NULL AND [ProductId] IS NOT NULL)"));

        builder.Property(s => s.Code).IsRequired().HasMaxLength(50);
        builder.Property(s => s.SignedQuantity).HasPrecision(18, 4);
        builder.Property(s => s.ReferenceType).HasMaxLength(50);
        builder.Property(s => s.ReferenceCode).HasMaxLength(50);
        builder.Property(s => s.Notes).HasMaxLength(2000);

        builder.Property(s => s.MovementType)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.HasIndex(s => s.Code).IsUnique();
        builder.HasIndex(s => new { s.RawMaterialId, s.WarehouseId });
        builder.HasIndex(s => new { s.ProductId, s.WarehouseId });
        builder.HasIndex(s => s.MovementType);
        builder.HasIndex(s => s.MovementDate);
        builder.HasIndex(s => new { s.ReferenceType, s.ReferenceId });

        builder.HasOne(s => s.RawMaterial)
            .WithMany()
            .HasForeignKey(s => s.RawMaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Product)
            .WithMany()
            .HasForeignKey(s => s.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Warehouse)
            .WithMany()
            .HasForeignKey(s => s.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Optional lot tag — additive, no cascade (a movement is never deleted with its lot)
        builder.HasOne(s => s.Lot)
            .WithMany()
            .HasForeignKey(s => s.LotId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(s => s.LotId);

        builder.Property(s => s.RowVersion).IsRowVersion();
    }
}

public class StockLotConfiguration : IEntityTypeConfiguration<StockLot>
{
    public void Configure(EntityTypeBuilder<StockLot> builder)
    {
        builder.ToTable("StockLots", t => t.HasCheckConstraint(
            "CK_StockLot_OneItemType",
            "([RawMaterialId] IS NOT NULL AND [ProductId] IS NULL) OR ([RawMaterialId] IS NULL AND [ProductId] IS NOT NULL)"));

        builder.Property(s => s.Code).IsRequired().HasMaxLength(50);
        builder.Property(s => s.LotNumber).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Shade).HasMaxLength(100);
        builder.Property(s => s.SourceType).HasMaxLength(50);
        builder.Property(s => s.SourceCode).HasMaxLength(50);
        builder.Property(s => s.Notes).HasMaxLength(2000);

        builder.Property(s => s.InitialQuantity).HasPrecision(18, 4);
        builder.Property(s => s.CurrentQuantity).HasPrecision(18, 4);

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(s => s.Code).IsUnique();
        builder.HasIndex(s => s.LotNumber);
        builder.HasIndex(s => new { s.RawMaterialId, s.WarehouseId });
        builder.HasIndex(s => new { s.ProductId, s.WarehouseId });
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.ExpiryDate);

        builder.HasOne(s => s.RawMaterial)
            .WithMany()
            .HasForeignKey(s => s.RawMaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Product)
            .WithMany()
            .HasForeignKey(s => s.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Warehouse)
            .WithMany()
            .HasForeignKey(s => s.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Supplier)
            .WithMany()
            .HasForeignKey(s => s.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(s => s.RowVersion).IsRowVersion();
    }
}

public class StockAdjustmentConfiguration : IEntityTypeConfiguration<StockAdjustment>
{
    public void Configure(EntityTypeBuilder<StockAdjustment> builder)
    {
        builder.ToTable("StockAdjustments");

        builder.Property(s => s.Code).IsRequired().HasMaxLength(50);
        builder.Property(s => s.Reason).IsRequired().HasMaxLength(200);
        builder.Property(s => s.PostedBy).HasMaxLength(100);
        builder.Property(s => s.Notes).HasMaxLength(2000);

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(s => s.Code).IsUnique();
        builder.HasIndex(s => s.WarehouseId);
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.AdjustmentDate);

        builder.HasOne(s => s.Warehouse)
            .WithMany()
            .HasForeignKey(s => s.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Lines)
            .WithOne(l => l.StockAdjustment)
            .HasForeignKey(l => l.StockAdjustmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(s => s.RowVersion).IsRowVersion();
    }
}

public class StockAdjustmentLineConfiguration : IEntityTypeConfiguration<StockAdjustmentLine>
{
    public void Configure(EntityTypeBuilder<StockAdjustmentLine> builder)
    {
        builder.ToTable("StockAdjustmentLines");

        builder.Property(l => l.SignedQuantity).HasPrecision(18, 4);
        builder.Property(l => l.LineNotes).HasMaxLength(1000);

        builder.HasIndex(l => l.StockAdjustmentId);
        builder.HasIndex(l => l.RawMaterialId);

        builder.HasOne(l => l.RawMaterial)
            .WithMany()
            .HasForeignKey(l => l.RawMaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(l => l.RowVersion).IsRowVersion();
    }
}
