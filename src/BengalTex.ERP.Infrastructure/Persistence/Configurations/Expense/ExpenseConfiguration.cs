using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Expense;

public class ExpenseCategoryConfiguration : IEntityTypeConfiguration<ExpenseCategory>
{
    public void Configure(EntityTypeBuilder<ExpenseCategory> builder)
    {
        builder.ToTable("ExpenseCategories");

        builder.Property(c => c.Name).IsRequired().HasMaxLength(150);
        builder.Property(c => c.Description).HasMaxLength(500);

        builder.HasIndex(c => c.Name);

        builder.HasOne(c => c.LedgerAccount)
            .WithMany()
            .HasForeignKey(c => c.LedgerAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(c => c.RowVersion).IsRowVersion();
    }
}

public class ExpenseConfiguration : IEntityTypeConfiguration<Domain.Entities.Expense>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Expense> builder)
    {
        builder.ToTable("Expenses");

        builder.Property(e => e.Code).IsRequired().HasMaxLength(50);
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.Payee).HasMaxLength(200);
        builder.Property(e => e.ReferenceNumber).HasMaxLength(100);
        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.Property(e => e.ApprovedBy).HasMaxLength(100);

        builder.Property(e => e.PaymentMethod).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(e => e.Code).IsUnique();
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.ExpenseDate);
        builder.HasIndex(e => e.ExpenseCategoryId);

        builder.HasOne(e => e.ExpenseCategory)
            .WithMany()
            .HasForeignKey(e => e.ExpenseCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.RowVersion).IsRowVersion();
    }
}
