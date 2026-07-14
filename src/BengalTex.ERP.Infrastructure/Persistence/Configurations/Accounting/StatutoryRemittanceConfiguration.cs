using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Accounting;

public class StatutoryRemittanceConfiguration : IEntityTypeConfiguration<StatutoryRemittance>
{
    public void Configure(EntityTypeBuilder<StatutoryRemittance> builder)
    {
        builder.ToTable("StatutoryRemittances");

        builder.Property(r => r.Code).IsRequired().HasMaxLength(50);
        builder.Property(r => r.TaxType).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.PaymentMethod).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Amount).HasPrecision(18, 2);
        builder.Property(r => r.ChallanNo).HasMaxLength(100);
        builder.Property(r => r.Notes).HasMaxLength(1000);

        builder.HasIndex(r => r.Code).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(r => new { r.TaxType, r.RemittanceDate });

        builder.Property(r => r.RowVersion).IsRowVersion();
    }
}
