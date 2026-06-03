using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PurchaseRequisitionEntity = BengalTex.ERP.Domain.Entities.PurchaseRequisition;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.PurchaseRequisition;

public class PurchaseRequisitionConfiguration : IEntityTypeConfiguration<PurchaseRequisitionEntity>
{
    public void Configure(EntityTypeBuilder<PurchaseRequisitionEntity> builder)
    {
        builder.ToTable("PurchaseRequisitions");

        builder.Property(p => p.Code).IsRequired().HasMaxLength(50);
        builder.Property(p => p.DepartmentText).HasMaxLength(100);
        builder.Property(p => p.RequestedBy).HasMaxLength(100);
        builder.Property(p => p.Purpose).HasMaxLength(500);
        builder.Property(p => p.Notes).HasMaxLength(2000);
        builder.Property(p => p.SubmittedByUser).HasMaxLength(100);
        builder.Property(p => p.DecidedByUser).HasMaxLength(100);
        builder.Property(p => p.DecisionNotes).HasMaxLength(1000);

        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(p => p.Code).IsUnique();
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.DepartmentId);
        builder.HasIndex(p => p.RequisitionDate);

        builder.HasOne(p => p.Department).WithMany()
            .HasForeignKey(p => p.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(p => p.ConvertedPurchaseOrder).WithMany()
            .HasForeignKey(p => p.ConvertedPurchaseOrderId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(p => p.Lines)
            .WithOne(l => l.PurchaseRequisition)
            .HasForeignKey(l => l.PurchaseRequisitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(p => p.RowVersion).IsRowVersion();
    }
}

public class PurchaseRequisitionLineConfiguration : IEntityTypeConfiguration<PurchaseRequisitionLine>
{
    public void Configure(EntityTypeBuilder<PurchaseRequisitionLine> builder)
    {
        builder.ToTable("PurchaseRequisitionLines");

        builder.Property(l => l.Quantity).HasPrecision(18, 4);
        builder.Property(l => l.EstimatedUnitPrice).HasPrecision(18, 4);
        builder.Property(l => l.LineNotes).HasMaxLength(1000);

        builder.HasIndex(l => l.PurchaseRequisitionId);
        builder.HasIndex(l => l.RawMaterialId);

        builder.HasOne(l => l.RawMaterial).WithMany()
            .HasForeignKey(l => l.RawMaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(l => l.RowVersion).IsRowVersion();
    }
}
