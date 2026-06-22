using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.MasterSetup;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");
        builder.Property(d => d.Code).HasMaxLength(30);
        builder.Property(d => d.Name).IsRequired().HasMaxLength(150);
        builder.Property(d => d.Description).HasMaxLength(500);

        builder.HasIndex(d => d.Name);
        builder.HasIndex(d => d.IsActive);

        builder.HasOne(d => d.ParentDepartment)
            .WithMany()
            .HasForeignKey(d => d.ParentDepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.HeadEmployee)
            .WithMany()
            .HasForeignKey(d => d.HeadEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(d => d.RowVersion).IsRowVersion();
    }
}

public class DesignationConfiguration : IEntityTypeConfiguration<Designation>
{
    public void Configure(EntityTypeBuilder<Designation> builder)
    {
        builder.ToTable("Designations");
        builder.Property(d => d.Code).HasMaxLength(30);
        builder.Property(d => d.Name).IsRequired().HasMaxLength(150);
        builder.Property(d => d.Description).HasMaxLength(500);
        builder.Property(d => d.AccessRoleName).HasMaxLength(256);

        builder.HasIndex(d => d.Name);
        builder.HasIndex(d => d.IsActive);

        builder.Property(d => d.RowVersion).IsRowVersion();
    }
}

public class ShiftConfiguration : IEntityTypeConfiguration<Shift>
{
    public void Configure(EntityTypeBuilder<Shift> builder)
    {
        builder.ToTable("Shifts");
        builder.Property(s => s.Code).IsRequired().HasMaxLength(20);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Description).HasMaxLength(500);

        builder.Property(s => s.WeekendDayOfWeek).HasConversion<string>().HasMaxLength(15);
        builder.Property(s => s.SecondWeekendDayOfWeek).HasConversion<string>().HasMaxLength(15);

        builder.HasIndex(s => s.Code).IsUnique();
        builder.HasIndex(s => s.IsActive);

        builder.Property(s => s.RowVersion).IsRowVersion();
    }
}

public class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.ToTable("BankAccounts");
        builder.Property(b => b.AccountName).IsRequired().HasMaxLength(200);
        builder.Property(b => b.BankName).IsRequired().HasMaxLength(150);
        builder.Property(b => b.BranchName).HasMaxLength(150);
        builder.Property(b => b.AccountNumber).IsRequired().HasMaxLength(50);
        builder.Property(b => b.RoutingNumber).HasMaxLength(30);
        builder.Property(b => b.SwiftCode).HasMaxLength(20);
        builder.Property(b => b.Currency).IsRequired().HasMaxLength(3);
        builder.Property(b => b.Notes).HasMaxLength(1000);

        builder.Property(b => b.AccountType).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(b => b.AccountNumber);
        builder.HasIndex(b => b.IsActive);

        builder.HasOne(b => b.LedgerAccount)
            .WithMany()
            .HasForeignKey(b => b.LedgerAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(b => b.RowVersion).IsRowVersion();
    }
}
