using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Employee;

public class PayslipConfiguration : IEntityTypeConfiguration<Payslip>
{
    public void Configure(EntityTypeBuilder<Payslip> builder)
    {
        builder.ToTable("Payslips");

        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.PaidBy).HasMaxLength(100);
        builder.Property(p => p.Notes).HasMaxLength(1000);

        foreach (var money in new[] {
                     nameof(Payslip.BasicSalary), nameof(Payslip.OvertimeAmount),
                     nameof(Payslip.Allowances), nameof(Payslip.Deductions),
                     nameof(Payslip.HouseRent), nameof(Payslip.Medical), nameof(Payslip.Transport),
                     nameof(Payslip.FoodAllowance), nameof(Payslip.FestivalBonus),
                     nameof(Payslip.PfEmployee), nameof(Payslip.PfEmployer),
                     nameof(Payslip.IncomeTax), nameof(Payslip.LoanDeduction),
                     nameof(Payslip.GrossPay), nameof(Payslip.NetPay) })
            builder.Property(money).HasPrecision(18, 2);

        foreach (var qty in new[] { nameof(Payslip.PresentDays), nameof(Payslip.AbsentDays),
                     nameof(Payslip.LeaveDays), nameof(Payslip.OvertimeHours) })
            builder.Property(qty).HasPrecision(9, 2);

        // One payslip per employee per month
        builder.HasIndex(p => new { p.EmployeeId, p.Year, p.Month }).IsUnique();
        builder.HasIndex(p => new { p.Year, p.Month });
        builder.HasIndex(p => p.Status);

        builder.HasOne(p => p.Employee)
            .WithMany()
            .HasForeignKey(p => p.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(p => p.RowVersion).IsRowVersion();
    }
}
