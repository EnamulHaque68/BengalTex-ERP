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

        // ── Upgrade fields ──
        builder.Property(a => a.Mode).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.FaceMatchStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.FaceMatchScore).HasPrecision(5, 2);
        builder.Property(a => a.ApprovalStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.CheckInSelfieUrl).HasMaxLength(500);
        builder.Property(a => a.CheckOutSelfieUrl).HasMaxLength(500);
        builder.Property(a => a.RejectionReason).HasMaxLength(1000);

        // ── Location & network intelligence (P2) ──
        builder.Property(a => a.CheckInAddress).HasMaxLength(500);
        builder.Property(a => a.CheckOutAddress).HasMaxLength(500);
        builder.Property(a => a.CheckInIpAddress).HasMaxLength(64);
        builder.Property(a => a.CheckInDeviceType).HasMaxLength(20);
        builder.Property(a => a.CheckInBrowser).HasMaxLength(60);
        builder.Property(a => a.CheckInOs).HasMaxLength(60);
        builder.Property(a => a.CheckInIsp).HasMaxLength(120);
        builder.Property(a => a.CheckInNetworkNote).HasMaxLength(120);

        // Geo-fence: SQL Server stores double as float(53) by default — fine for lat/lng/meters
        builder.HasIndex(a => a.CheckInWithinFence);
        builder.HasIndex(a => a.ApprovalStatus);
        builder.HasIndex(a => a.CheckInIsProxyVpn);

        // One LIVE attendance row per employee per date (filtered so a soft-deleted row doesn't
        // block re-creating that date — the "already exists" checks are soft-delete-filtered).
        builder.HasIndex(a => new { a.EmployeeId, a.AttendanceDate }).IsUnique()
            .HasFilter("[IsDeleted] = 0");
        builder.HasIndex(a => a.AttendanceDate);

        builder.HasOne(a => a.Employee)
            .WithMany()
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Matched office location (multi-location geo-fence) — SetNull so deleting a location keeps history
        builder.HasOne(a => a.MatchedOfficeLocation)
            .WithMany()
            .HasForeignKey(a => a.MatchedOfficeLocationId)
            .OnDelete(DeleteBehavior.SetNull);

        // Approver (NoAction — avoids multiple-cascade-path with the Employee FK)
        builder.HasOne(a => a.ApprovedByEmployee)
            .WithMany()
            .HasForeignKey(a => a.ApprovedByEmployeeId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(a => a.Breaks)
            .WithOne(b => b.AttendanceRecord)
            .HasForeignKey(b => b.AttendanceRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(a => a.RowVersion).IsRowVersion();
    }
}
