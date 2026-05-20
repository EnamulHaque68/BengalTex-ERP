using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.QcInspection;

public class QcInspectionConfiguration : IEntityTypeConfiguration<Domain.Entities.QcInspection>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.QcInspection> builder)
    {
        builder.ToTable("QcInspections", t => t.HasCheckConstraint(
            "CK_QcInspection_OneSource",
            "([GoodsReceiptNoteId] IS NOT NULL AND [ProductionOrderId] IS NULL) OR ([GoodsReceiptNoteId] IS NULL AND [ProductionOrderId] IS NOT NULL)"));

        builder.Property(q => q.Code).IsRequired().HasMaxLength(50);
        builder.Property(q => q.InspectedBy).HasMaxLength(100);
        builder.Property(q => q.Notes).HasMaxLength(2000);

        builder.Property(q => q.SourceType).HasConversion<string>().HasMaxLength(20);
        builder.Property(q => q.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(q => q.OverallResult).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(q => q.Code).IsUnique();
        builder.HasIndex(q => q.GoodsReceiptNoteId);
        builder.HasIndex(q => q.ProductionOrderId);
        builder.HasIndex(q => q.Status);
        builder.HasIndex(q => q.InspectionDate);

        builder.HasOne(q => q.GoodsReceiptNote)
            .WithMany()
            .HasForeignKey(q => q.GoodsReceiptNoteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(q => q.ProductionOrder)
            .WithMany()
            .HasForeignKey(q => q.ProductionOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(q => q.InspectedFromWarehouse)
            .WithMany()
            .HasForeignKey(q => q.InspectedFromWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(q => q.QuarantineWarehouse)
            .WithMany()
            .HasForeignKey(q => q.QuarantineWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(q => q.Lines)
            .WithOne(l => l.QcInspection)
            .HasForeignKey(l => l.QcInspectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(q => q.RowVersion).IsRowVersion();
    }
}

public class QcInspectionLineConfiguration : IEntityTypeConfiguration<QcInspectionLine>
{
    public void Configure(EntityTypeBuilder<QcInspectionLine> builder)
    {
        builder.ToTable("QcInspectionLines", t => t.HasCheckConstraint(
            "CK_QcInspectionLine_OneItemType",
            "([RawMaterialId] IS NOT NULL AND [ProductId] IS NULL) OR ([RawMaterialId] IS NULL AND [ProductId] IS NOT NULL)"));

        builder.Property(l => l.InspectedQuantity).HasPrecision(18, 4);
        builder.Property(l => l.PassedQuantity).HasPrecision(18, 4);
        builder.Property(l => l.RejectedQuantity).HasPrecision(18, 4);
        builder.Property(l => l.DefectNotes).HasMaxLength(1000);

        builder.HasIndex(l => l.QcInspectionId);
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
