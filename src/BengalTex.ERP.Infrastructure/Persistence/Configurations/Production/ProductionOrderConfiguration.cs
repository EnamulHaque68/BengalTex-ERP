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

        // Phase 6 — cost sheet (base BDT)
        builder.Property(p => p.MaterialCost).HasPrecision(18, 2);
        builder.Property(p => p.LabourCost).HasPrecision(18, 2);
        builder.Property(p => p.MachineCost).HasPrecision(18, 2);
        builder.Property(p => p.OverheadCost).HasPrecision(18, 2);
        builder.Property(p => p.SubcontractCost).HasPrecision(18, 2);
        builder.Property(p => p.WastageCost).HasPrecision(18, 2);
        builder.Property(p => p.RejectCost).HasPrecision(18, 2);

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(p => p.Code).IsUnique();
        builder.HasIndex(p => p.ProductId);
        builder.HasIndex(p => p.BomId);
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.PlannedStartDate);
        builder.HasIndex(p => p.SalesOrderId);

        // Optional source Sales Order link (Phase 1 — Sales-driven production). Nullable FKs,
        // NoAction on delete so a sales order/line with production history can't be hard-deleted
        // and no multiple-cascade-path is introduced. Standalone runs leave both null.
        builder.HasOne(p => p.SalesOrder)
            .WithMany()
            .HasForeignKey(p => p.SalesOrderId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(p => p.SalesOrderLine)
            .WithMany()
            .HasForeignKey(p => p.SalesOrderLineId)
            .OnDelete(DeleteBehavior.NoAction);

        // Product FK — Restrict so a product with production orders can't be deleted
        builder.HasOne(p => p.Product)
            .WithMany()
            .HasForeignKey(p => p.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Phase A3 — optional style dimension
        builder.HasOne(p => p.Style).WithMany().HasForeignKey(p => p.StyleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => p.StyleId);

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

        // One-to-many: order → routing stages
        builder.HasMany(p => p.Stages)
            .WithOne(s => s.ProductionOrder)
            .HasForeignKey(s => s.ProductionOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(p => p.RowVersion).IsRowVersion();
    }
}

public class ProductionStageConfiguration : IEntityTypeConfiguration<ProductionStage>
{
    public void Configure(EntityTypeBuilder<ProductionStage> builder)
    {
        builder.ToTable("ProductionStages");

        builder.Property(s => s.StageName).IsRequired().HasMaxLength(100);
        builder.Property(s => s.ProductionLine).HasMaxLength(100);
        builder.Property(s => s.Notes).HasMaxLength(1000);

        builder.Property(s => s.PlannedQuantity).HasPrecision(18, 4);
        builder.Property(s => s.CompletedQuantity).HasPrecision(18, 4);
        builder.Property(s => s.RejectedQuantity).HasPrecision(18, 4);

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(s => s.ProductionOrderId);
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.WorkCenterId);

        // Operator → Employee (optional) — Restrict so an employee with stage history isn't deleted
        builder.HasOne(s => s.OperatorEmployee)
            .WithMany()
            .HasForeignKey(s => s.OperatorEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Work center (optional, Phase 4 capacity) — Restrict so a referenced work center can't be deleted
        builder.HasOne(s => s.WorkCenter)
            .WithMany()
            .HasForeignKey(s => s.WorkCenterId)
            .OnDelete(DeleteBehavior.Restrict);

        // Shift (optional, Phase 4 multi-shift) — Restrict
        builder.HasOne(s => s.Shift)
            .WithMany()
            .HasForeignKey(s => s.ShiftId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(s => s.RowVersion).IsRowVersion();
    }
}
