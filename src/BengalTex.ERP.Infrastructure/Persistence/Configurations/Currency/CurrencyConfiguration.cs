using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CurrencyEntity = BengalTex.ERP.Domain.Entities.Currency;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Currency;

public class CurrencyConfiguration : IEntityTypeConfiguration<CurrencyEntity>
{
    public void Configure(EntityTypeBuilder<CurrencyEntity> builder)
    {
        builder.ToTable("Currencies");

        builder.Property(c => c.Code).IsRequired().HasMaxLength(3);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Symbol).IsRequired().HasMaxLength(10);
        builder.Property(c => c.ExchangeRateToBase).HasPrecision(18, 6);

        builder.HasIndex(c => c.Code).IsUnique();
        builder.HasIndex(c => c.IsActive);

        // Only one base currency allowed across the system
        builder.HasIndex(c => c.IsBaseCurrency)
            .HasFilter("[IsBaseCurrency] = 1")
            .IsUnique()
            .HasDatabaseName("UX_Currencies_SingleBase");

        builder.Property(c => c.RowVersion).IsRowVersion();
    }
}
