using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Accounting;

public class BudgetConfiguration : IEntityTypeConfiguration<Budget>
{
    public void Configure(EntityTypeBuilder<Budget> builder)
    {
        builder.ToTable("Budgets");

        builder.Property(b => b.Code).IsRequired().HasMaxLength(50);
        builder.Property(b => b.Name).IsRequired().HasMaxLength(200);
        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(b => b.Notes).HasMaxLength(1000);

        builder.HasIndex(b => b.Code).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(b => b.FinancialYearId);

        builder.HasOne(b => b.FinancialYear)
            .WithMany()
            .HasForeignKey(b => b.FinancialYearId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(b => b.RowVersion).IsRowVersion();
    }
}

public class BudgetLineConfiguration : IEntityTypeConfiguration<BudgetLine>
{
    public void Configure(EntityTypeBuilder<BudgetLine> builder)
    {
        builder.ToTable("BudgetLines");

        foreach (var m in new[] { "M1", "M2", "M3", "M4", "M5", "M6", "M7", "M8", "M9", "M10", "M11", "M12" })
            builder.Property(m).HasPrecision(18, 2);

        builder.HasIndex(l => l.BudgetId);

        builder.HasOne(l => l.Budget)
            .WithMany(b => b.Lines)
            .HasForeignKey(l => l.BudgetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(l => l.Account)
            .WithMany()
            .HasForeignKey(l => l.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.CostCenter)
            .WithMany()
            .HasForeignKey(l => l.CostCenterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(l => l.RowVersion).IsRowVersion();
    }
}
