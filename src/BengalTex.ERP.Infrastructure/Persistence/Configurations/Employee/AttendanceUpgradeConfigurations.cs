using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Employee;

public class OfficeLocationConfiguration : IEntityTypeConfiguration<OfficeLocation>
{
    public void Configure(EntityTypeBuilder<OfficeLocation> builder)
    {
        builder.ToTable("OfficeLocations");

        builder.Property(o => o.Name).IsRequired().HasMaxLength(150);
        builder.Property(o => o.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.RadiusMeters).HasDefaultValue(10d);
        builder.Property(o => o.Address).HasMaxLength(500);

        builder.HasIndex(o => o.CompanyId);

        builder.HasOne(o => o.Company)
            .WithMany()
            .HasForeignKey(o => o.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(o => o.RowVersion).IsRowVersion();
    }
}

public class EmployeeOfficeLocationConfiguration : IEntityTypeConfiguration<EmployeeOfficeLocation>
{
    public void Configure(EntityTypeBuilder<EmployeeOfficeLocation> builder)
    {
        builder.ToTable("EmployeeOfficeLocations");

        builder.HasIndex(e => e.EmployeeId);
        builder.HasIndex(e => e.OfficeLocationId);
        // One assignment per (employee, location)
        builder.HasIndex(e => new { e.EmployeeId, e.OfficeLocationId })
            .HasFilter("[IsDeleted] = 0").IsUnique()
            .HasDatabaseName("UX_EmployeeOfficeLocations_Pair");

        builder.HasOne(e => e.Employee)
            .WithMany()
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.OfficeLocation)
            .WithMany()
            .HasForeignKey(e => e.OfficeLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.RowVersion).IsRowVersion();
    }
}

public class AttendanceBreakConfiguration : IEntityTypeConfiguration<AttendanceBreak>
{
    public void Configure(EntityTypeBuilder<AttendanceBreak> builder)
    {
        builder.ToTable("AttendanceBreaks");

        builder.Property(b => b.BreakOutTime).HasMaxLength(10);
        builder.Property(b => b.BreakInTime).HasMaxLength(10);
        builder.HasIndex(b => b.AttendanceRecordId);

        builder.Property(b => b.RowVersion).IsRowVersion();
    }
}

public class AttendanceRequestConfiguration : IEntityTypeConfiguration<AttendanceRequest>
{
    public void Configure(EntityTypeBuilder<AttendanceRequest> builder)
    {
        builder.ToTable("AttendanceRequests");

        builder.Property(r => r.RequestType).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.RequestedStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.RequestedCheckInTime).HasMaxLength(10);
        builder.Property(r => r.RequestedCheckOutTime).HasMaxLength(10);
        builder.Property(r => r.Reason).IsRequired().HasMaxLength(1000);
        builder.Property(r => r.ReviewNote).HasMaxLength(1000);

        builder.HasIndex(r => r.EmployeeId);
        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => new { r.EmployeeId, r.RequestDate });

        builder.HasOne(r => r.Employee)
            .WithMany()
            .HasForeignKey(r => r.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Reviewer (NoAction — avoids multiple-cascade-path with the Employee FK)
        builder.HasOne(r => r.ReviewedByEmployee)
            .WithMany()
            .HasForeignKey(r => r.ReviewedByEmployeeId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Property(r => r.RowVersion).IsRowVersion();
    }
}

public class AttendanceSettingsConfiguration : IEntityTypeConfiguration<AttendanceSettings>
{
    public void Configure(EntityTypeBuilder<AttendanceSettings> builder)
    {
        builder.ToTable("AttendanceSettings");

        builder.Property(s => s.OutsideFenceMode).HasConversion<string>().HasMaxLength(20);
        // One settings row per company
        builder.HasIndex(s => s.CompanyId).IsUnique();

        builder.HasOne(s => s.Company)
            .WithMany()
            .HasForeignKey(s => s.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(s => s.RowVersion).IsRowVersion();
    }
}
