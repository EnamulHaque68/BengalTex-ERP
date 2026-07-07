using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Accounting;

public class CostingRateConfiguration : IEntityTypeConfiguration<CostingRate>
{
    public void Configure(EntityTypeBuilder<CostingRate> builder)
    {
        builder.ToTable("CostingRates");

        builder.Property(r => r.RateType).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Basis).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.Rate).HasPrecision(18, 6);
        builder.Property(r => r.Notes).HasMaxLength(500);

        builder.HasIndex(r => new { r.RateType, r.WorkCenterId, r.EffectiveFrom });

        builder.HasOne(r => r.WorkCenter)
            .WithMany()
            .HasForeignKey(r => r.WorkCenterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.RowVersion).IsRowVersion();
    }
}
