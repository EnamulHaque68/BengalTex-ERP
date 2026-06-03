using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Employee;

public class FinalSettlementConfiguration : IEntityTypeConfiguration<FinalSettlement>
{
    public void Configure(EntityTypeBuilder<FinalSettlement> builder)
    {
        builder.ToTable("FinalSettlements");

        builder.Property(s => s.Code).IsRequired().HasMaxLength(50);
        builder.Property(s => s.Notes).HasMaxLength(2000);
        builder.Property(s => s.PaymentMethod).HasMaxLength(30);
        builder.Property(s => s.PaymentReference).HasMaxLength(100);
        builder.Property(s => s.ApprovedByUser).HasMaxLength(256);

        builder.Property(s => s.BasicSalary).HasPrecision(18, 2);
        builder.Property(s => s.ProratedDays).HasPrecision(8, 2);
        builder.Property(s => s.ProratedSalary).HasPrecision(18, 2);
        builder.Property(s => s.LeaveEncashmentDays).HasPrecision(8, 2);
        builder.Property(s => s.LeaveEncashmentAmount).HasPrecision(18, 2);
        builder.Property(s => s.YearsOfService).HasPrecision(8, 2);
        builder.Property(s => s.GratuityAmount).HasPrecision(18, 2);
        builder.Property(s => s.OtherEarnings).HasPrecision(18, 2);
        builder.Property(s => s.OutstandingLoan).HasPrecision(18, 2);
        builder.Property(s => s.OtherDeductions).HasPrecision(18, 2);
        builder.Property(s => s.GrossPayable).HasPrecision(18, 2);
        builder.Property(s => s.TotalDeductions).HasPrecision(18, 2);
        builder.Property(s => s.NetPayable).HasPrecision(18, 2);

        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.Reason).HasConversion<string>().HasMaxLength(30);

        builder.HasIndex(s => s.Code).IsUnique();
        builder.HasIndex(s => s.EmployeeId);
        builder.HasIndex(s => s.Status);

        builder.HasOne(s => s.Employee).WithMany()
            .HasForeignKey(s => s.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(s => s.RowVersion).IsRowVersion();
    }
}
