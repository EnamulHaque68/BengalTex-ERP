using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EmployeeEntity = BengalTex.ERP.Domain.Entities.Employee;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Employee;

public class EmployeeConfiguration : IEntityTypeConfiguration<EmployeeEntity>
{
    public void Configure(EntityTypeBuilder<EmployeeEntity> builder)
    {
        builder.ToTable("Employees");

        builder.Property(e => e.Code).IsRequired().HasMaxLength(50);
        builder.Property(e => e.FullName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Designation).HasMaxLength(100);
        builder.Property(e => e.Department).HasMaxLength(100);
        builder.Property(e => e.Phone).HasMaxLength(30);
        builder.Property(e => e.Email).HasMaxLength(200);
        builder.Property(e => e.NationalId).HasMaxLength(50);
        builder.Property(e => e.Address).HasMaxLength(500);

        builder.Property(e => e.Gender).HasConversion<string>().HasMaxLength(10);
        builder.Property(e => e.EmploymentType).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);

        builder.Property(e => e.BasicSalary).HasPrecision(18, 2);
        builder.Property(e => e.HouseRentAllowance).HasPrecision(18, 2);
        builder.Property(e => e.MedicalAllowance).HasPrecision(18, 2);
        builder.Property(e => e.TransportAllowance).HasPrecision(18, 2);
        builder.Property(e => e.FoodAllowance).HasPrecision(18, 2);
        builder.Property(e => e.PfRate).HasPrecision(5, 2);
        builder.Property(e => e.Notes).HasMaxLength(2000);

        builder.HasIndex(e => e.Code).IsUnique();
        builder.HasIndex(e => e.FullName);
        builder.HasIndex(e => e.Department);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.IsActive);

        // ── Master-Setup FKs (v1a optional; legacy free-text fields preserved above) ──
        builder.HasOne(e => e.DepartmentEntity)
            .WithMany()
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.DesignationEntity)
            .WithMany()
            .HasForeignKey(e => e.DesignationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Shift)
            .WithMany()
            .HasForeignKey(e => e.ShiftId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.BankAccount)
            .WithMany()
            .HasForeignKey(e => e.BankAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.DepartmentId);
        builder.HasIndex(e => e.DesignationId);
        builder.HasIndex(e => e.ShiftId);
        builder.HasIndex(e => e.BankAccountId);

        builder.Property(e => e.RowVersion).IsRowVersion();
    }
}
