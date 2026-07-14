using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Accounting;

public class ExchangeRateConfiguration : IEntityTypeConfiguration<ExchangeRate>
{
    public void Configure(EntityTypeBuilder<ExchangeRate> builder)
    {
        builder.ToTable("ExchangeRates");

        builder.Property(r => r.Rate).HasPrecision(18, 6);
        builder.Property(r => r.Source).HasMaxLength(100);

        // One rate per currency per date (filtered so a soft-deleted row doesn't block re-entry).
        builder.HasIndex(r => new { r.CurrencyId, r.RateDate }).IsUnique().HasFilter("[IsDeleted] = 0");

        builder.HasOne(r => r.Currency)
            .WithMany()
            .HasForeignKey(r => r.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
