using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Employee;

public class EmployeeHistoryEntryConfiguration : IEntityTypeConfiguration<EmployeeHistoryEntry>
{
    public void Configure(EntityTypeBuilder<EmployeeHistoryEntry> builder)
    {
        builder.ToTable("EmployeeHistoryEntries");

        builder.Property(h => h.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(h => h.Title).IsRequired().HasMaxLength(200);
        builder.Property(h => h.FromValue).HasMaxLength(200);
        builder.Property(h => h.ToValue).HasMaxLength(200);
        builder.Property(h => h.Amount).HasPrecision(18, 2);
        builder.Property(h => h.Details).HasMaxLength(2000);

        builder.HasIndex(h => h.EmployeeId);
        builder.HasIndex(h => h.Type);

        builder.HasOne(h => h.Employee)
            .WithMany()
            .HasForeignKey(h => h.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(h => h.RowVersion).IsRowVersion();
    }
}
