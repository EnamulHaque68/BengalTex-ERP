using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Employee;

public class EmployeeLoanConfiguration : IEntityTypeConfiguration<EmployeeLoan>
{
    public void Configure(EntityTypeBuilder<EmployeeLoan> builder)
    {
        builder.ToTable("EmployeeLoans");

        builder.Property(l => l.Code).IsRequired().HasMaxLength(50);
        builder.Property(l => l.Notes).HasMaxLength(1000);

        builder.Property(l => l.Principal).HasPrecision(18, 2);
        builder.Property(l => l.EmiAmount).HasPrecision(18, 2);
        builder.Property(l => l.OutstandingPrincipal).HasPrecision(18, 2);

        builder.Property(l => l.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(l => l.Code).IsUnique();
        builder.HasIndex(l => l.EmployeeId);
        builder.HasIndex(l => l.Status);

        builder.HasOne(l => l.Employee).WithMany()
            .HasForeignKey(l => l.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(l => l.RowVersion).IsRowVersion();
    }
}

public class FestivalBonusConfiguration : IEntityTypeConfiguration<FestivalBonus>
{
    public void Configure(EntityTypeBuilder<FestivalBonus> builder)
    {
        builder.ToTable("FestivalBonuses");

        builder.Property(b => b.Code).IsRequired().HasMaxLength(50);
        builder.Property(b => b.PaidBy).HasMaxLength(100);
        builder.Property(b => b.Notes).HasMaxLength(1000);

        builder.Property(b => b.Amount).HasPrecision(18, 2);
        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(b => b.BonusType).HasConversion<string>().HasMaxLength(30);
        builder.Property(b => b.PaymentMethod).HasConversion<string>().HasMaxLength(30);

        builder.HasIndex(b => b.Code).IsUnique();
        builder.HasIndex(b => new { b.EmployeeId, b.BonusYear, b.BonusType }).IsUnique()
            .HasFilter("[IsDeleted] = 0");   // filtered so a soft-deleted bonus doesn't block re-creating it
        builder.HasIndex(b => new { b.BonusYear, b.BonusType });
        builder.HasIndex(b => b.Status);

        builder.HasOne(b => b.Employee).WithMany()
            .HasForeignKey(b => b.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(b => b.RowVersion).IsRowVersion();
    }
}
