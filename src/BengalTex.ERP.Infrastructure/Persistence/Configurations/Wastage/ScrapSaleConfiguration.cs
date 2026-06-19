using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Wastage;

public class ScrapSaleConfiguration : IEntityTypeConfiguration<ScrapSale>
{
    public void Configure(EntityTypeBuilder<ScrapSale> builder)
    {
        builder.ToTable("ScrapSales");

        builder.Property(s => s.Code).IsRequired().HasMaxLength(50);
        builder.Property(s => s.BuyerName).HasMaxLength(200);
        builder.Property(s => s.PostedBy).HasMaxLength(100);
        builder.Property(s => s.Notes).HasMaxLength(2000);
        builder.Property(s => s.PaymentMethod).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(s => s.Code).IsUnique();
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.SaleDate);

        builder.HasMany(s => s.Lines)
            .WithOne(l => l.ScrapSale)
            .HasForeignKey(l => l.ScrapSaleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(s => s.RowVersion).IsRowVersion();
    }
}

public class ScrapSaleLineConfiguration : IEntityTypeConfiguration<ScrapSaleLine>
{
    public void Configure(EntityTypeBuilder<ScrapSaleLine> builder)
    {
        builder.ToTable("ScrapSaleLines");

        builder.Property(l => l.Description).IsRequired().HasMaxLength(300);
        builder.Property(l => l.Quantity).HasPrecision(18, 4);
        builder.Property(l => l.UnitPrice).HasPrecision(18, 2);
        builder.Property(l => l.Unit).HasMaxLength(20);

        builder.HasIndex(l => l.ScrapSaleId);

        builder.Property(l => l.RowVersion).IsRowVersion();
    }
}
