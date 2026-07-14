using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Banking;

public class ExportIncentiveClaimConfiguration : IEntityTypeConfiguration<ExportIncentiveClaim>
{
    public void Configure(EntityTypeBuilder<ExportIncentiveClaim> builder)
    {
        builder.ToTable("ExportIncentiveClaims");

        builder.Property(e => e.Code).IsRequired().HasMaxLength(50);
        builder.Property(e => e.ExportReference).HasMaxLength(100);
        builder.Property(e => e.IncentiveRate).HasPrecision(9, 4);
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.ReceivedMethod).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.BankReference).HasMaxLength(100);
        builder.Property(e => e.Notes).HasMaxLength(1000);

        builder.HasIndex(e => e.Code).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(e => e.Status);

        builder.HasOne(e => e.CustomerInvoice)
            .WithMany()
            .HasForeignKey(e => e.CustomerInvoiceId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(e => e.RowVersion).IsRowVersion();
    }
}
