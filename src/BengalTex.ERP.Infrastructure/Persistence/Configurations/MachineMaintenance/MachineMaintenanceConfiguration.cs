using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.MachineMaintenance;

public class MachineMaintenanceConfiguration : IEntityTypeConfiguration<Domain.Entities.MachineMaintenance>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.MachineMaintenance> builder)
    {
        builder.ToTable("MachineMaintenances");

        builder.Property(m => m.Code).IsRequired().HasMaxLength(50);
        builder.Property(m => m.Description).IsRequired().HasMaxLength(500);
        builder.Property(m => m.PerformedBy).HasMaxLength(150);
        builder.Property(m => m.PartsReplaced).HasMaxLength(1000);
        builder.Property(m => m.CompletionNotes).HasMaxLength(2000);
        builder.Property(m => m.Notes).HasMaxLength(2000);

        builder.Property(m => m.DowntimeHours).HasPrecision(8, 2);
        builder.Property(m => m.ServiceCost).HasPrecision(18, 2);
        builder.Property(m => m.PartsCost).HasPrecision(18, 2);

        // TotalCost is computed (no setter persistence)
        builder.Ignore(m => m.TotalCost);

        builder.Property(m => m.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(m => m.Code).IsUnique();
        builder.HasIndex(m => m.MachineId);
        builder.HasIndex(m => m.Status);
        builder.HasIndex(m => m.ScheduledDate);
        builder.HasIndex(m => m.RecurringSeriesAnchorId);

        builder.HasOne(m => m.Machine).WithMany()
            .HasForeignKey(m => m.MachineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.PerformedByEmployee).WithMany()
            .HasForeignKey(m => m.PerformedByEmployeeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(m => m.RecurringSeriesAnchor).WithMany()
            .HasForeignKey(m => m.RecurringSeriesAnchorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(m => m.RowVersion).IsRowVersion();
    }
}
