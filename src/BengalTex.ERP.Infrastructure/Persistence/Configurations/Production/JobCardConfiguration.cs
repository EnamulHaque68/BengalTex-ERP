using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Production;

public class JobCardConfiguration : IEntityTypeConfiguration<JobCard>
{
    public void Configure(EntityTypeBuilder<JobCard> builder)
    {
        builder.ToTable("JobCards");

        builder.Property(j => j.Code).IsRequired().HasMaxLength(50);
        builder.Property(j => j.BatchNumber).HasMaxLength(100);
        builder.Property(j => j.CompletedBy).HasMaxLength(100);
        builder.Property(j => j.Notes).HasMaxLength(2000);

        builder.Property(j => j.Status).HasConversion<string>().HasMaxLength(20);

        builder.Property(j => j.Quantity).HasPrecision(18, 4);
        builder.Property(j => j.CompletedQuantity).HasPrecision(18, 4);
        builder.Property(j => j.RejectedQuantity).HasPrecision(18, 4);

        builder.HasIndex(j => j.Code).IsUnique();
        builder.HasIndex(j => j.ProductionOrderId);
        builder.HasIndex(j => j.ProductionStageId);
        builder.HasIndex(j => j.Status);
        builder.HasIndex(j => j.MachineId);
        builder.HasIndex(j => j.OperatorEmployeeId);

        builder.HasOne(j => j.ProductionOrder)
            .WithMany()
            .HasForeignKey(j => j.ProductionOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(j => j.ProductionStage)
            .WithMany()
            .HasForeignKey(j => j.ProductionStageId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(j => j.Machine)
            .WithMany()
            .HasForeignKey(j => j.MachineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(j => j.OperatorEmployee)
            .WithMany()
            .HasForeignKey(j => j.OperatorEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(j => j.Scans)
            .WithOne(s => s.JobCard)
            .HasForeignKey(s => s.JobCardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(j => j.RowVersion).IsRowVersion();
    }
}

public class JobCardScanConfiguration : IEntityTypeConfiguration<JobCardScan>
{
    public void Configure(EntityTypeBuilder<JobCardScan> builder)
    {
        builder.ToTable("JobCardScans");

        builder.Property(s => s.ScanType).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.ScannedBy).HasMaxLength(100);
        builder.Property(s => s.Notes).HasMaxLength(1000);

        builder.Property(s => s.Quantity).HasPrecision(18, 4);
        builder.Property(s => s.RejectedQuantity).HasPrecision(18, 4);

        builder.HasIndex(s => s.JobCardId);
        builder.HasIndex(s => s.ScannedAt);

        builder.Property(s => s.RowVersion).IsRowVersion();
    }
}
