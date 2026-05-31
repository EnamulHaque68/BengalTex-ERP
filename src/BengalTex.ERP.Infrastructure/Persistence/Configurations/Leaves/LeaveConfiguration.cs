using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Leaves;

public class LeaveTypeConfiguration : IEntityTypeConfiguration<LeaveType>
{
    public void Configure(EntityTypeBuilder<LeaveType> builder)
    {
        builder.ToTable("LeaveTypes");
        builder.Property(t => t.Code).IsRequired().HasMaxLength(20);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);
        builder.Property(t => t.AnnualEntitlement).HasPrecision(9, 2);
        builder.Property(t => t.Description).HasMaxLength(500);
        builder.HasIndex(t => t.Code).IsUnique();
        builder.HasIndex(t => t.IsActive);
        builder.Property(t => t.RowVersion).IsRowVersion();
    }
}

public class HolidayConfiguration : IEntityTypeConfiguration<Holiday>
{
    public void Configure(EntityTypeBuilder<Holiday> builder)
    {
        builder.ToTable("Holidays");
        builder.Property(h => h.Name).IsRequired().HasMaxLength(150);
        builder.Property(h => h.Description).HasMaxLength(500);
        builder.HasIndex(h => h.Date);
        builder.HasIndex(h => new { h.Date, h.Name }).IsUnique();
        builder.Property(h => h.RowVersion).IsRowVersion();
    }
}

public class LeaveBalanceConfiguration : IEntityTypeConfiguration<LeaveBalance>
{
    public void Configure(EntityTypeBuilder<LeaveBalance> builder)
    {
        builder.ToTable("LeaveBalances");
        builder.Ignore(b => b.Remaining);    // computed in domain — not persisted
        builder.Property(b => b.Entitled).HasPrecision(9, 2);
        builder.Property(b => b.Taken).HasPrecision(9, 2);
        builder.HasIndex(b => new { b.EmployeeId, b.LeaveTypeId, b.Year }).IsUnique();
        builder.HasIndex(b => b.Year);

        builder.HasOne(b => b.Employee).WithMany()
            .HasForeignKey(b => b.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(b => b.LeaveType).WithMany()
            .HasForeignKey(b => b.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(b => b.RowVersion).IsRowVersion();
    }
}

public class LeaveApplicationConfiguration : IEntityTypeConfiguration<LeaveApplication>
{
    public void Configure(EntityTypeBuilder<LeaveApplication> builder)
    {
        builder.ToTable("LeaveApplications");
        builder.Property(a => a.Code).IsRequired().HasMaxLength(50);
        builder.Property(a => a.Reason).HasMaxLength(1000);
        builder.Property(a => a.DecidedBy).HasMaxLength(100);
        builder.Property(a => a.RejectionReason).HasMaxLength(500);
        builder.Property(a => a.Notes).HasMaxLength(1000);
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.TotalDays).HasPrecision(9, 2);

        builder.HasIndex(a => a.Code).IsUnique();
        builder.HasIndex(a => a.EmployeeId);
        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => new { a.FromDate, a.ToDate });

        builder.HasOne(a => a.Employee).WithMany()
            .HasForeignKey(a => a.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.LeaveType).WithMany()
            .HasForeignKey(a => a.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(a => a.RowVersion).IsRowVersion();
    }
}
