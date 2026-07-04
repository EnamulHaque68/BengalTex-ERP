using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Accounting;

public class FinancialYearConfiguration : IEntityTypeConfiguration<FinancialYear>
{
    public void Configure(EntityTypeBuilder<FinancialYear> builder)
    {
        builder.ToTable("FinancialYears");

        builder.Property(f => f.Code).IsRequired().HasMaxLength(20);
        builder.Property(f => f.ClosedBy).HasMaxLength(100);
        builder.Property(f => f.Notes).HasMaxLength(1000);

        builder.Property(f => f.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(f => f.Code).IsUnique();
        builder.HasIndex(f => f.Status);
        builder.HasIndex(f => new { f.StartDate, f.EndDate });

        builder.HasMany(f => f.Periods)
            .WithOne(p => p.FinancialYear)
            .HasForeignKey(p => p.FinancialYearId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(f => f.RowVersion).IsRowVersion();
    }
}

public class AccountingPeriodConfiguration : IEntityTypeConfiguration<AccountingPeriod>
{
    public void Configure(EntityTypeBuilder<AccountingPeriod> builder)
    {
        builder.ToTable("AccountingPeriods");

        builder.Property(p => p.Name).IsRequired().HasMaxLength(30);
        builder.Property(p => p.StatusChangedBy).HasMaxLength(100);

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(p => new { p.FinancialYearId, p.PeriodNumber }).IsUnique();
        builder.HasIndex(p => new { p.StartDate, p.EndDate });   // date-range lookup by the period guard
        builder.HasIndex(p => p.Status);

        builder.Property(p => p.RowVersion).IsRowVersion();
    }
}
