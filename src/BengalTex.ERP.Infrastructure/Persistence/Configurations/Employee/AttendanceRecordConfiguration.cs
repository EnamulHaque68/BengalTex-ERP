using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Employee;

public class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("AttendanceRecords");

        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.CheckInTime).HasMaxLength(10);
        builder.Property(a => a.CheckOutTime).HasMaxLength(10);
        builder.Property(a => a.OvertimeHours).HasPrecision(9, 2);
        builder.Property(a => a.Notes).HasMaxLength(1000);

        // One attendance row per employee per date
        builder.HasIndex(a => new { a.EmployeeId, a.AttendanceDate }).IsUnique();
        builder.HasIndex(a => a.AttendanceDate);

        builder.HasOne(a => a.Employee)
            .WithMany()
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(a => a.RowVersion).IsRowVersion();
    }
}
