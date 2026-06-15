using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Customer;

public class CustomerProductPriceConfiguration : IEntityTypeConfiguration<CustomerProductPrice>
{
    public void Configure(EntityTypeBuilder<CustomerProductPrice> builder)
    {
        builder.ToTable("CustomerProductPrices");

        builder.Property(p => p.UnitPrice).HasPrecision(18, 2);
        builder.Property(p => p.BuyerProductCode).HasMaxLength(50);
        builder.Property(p => p.BuyerProductName).HasMaxLength(200);
        builder.Property(p => p.Notes).HasMaxLength(1000);

        builder.HasOne(p => p.Customer)
            .WithMany()
            .HasForeignKey(p => p.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Product)
            .WithMany()
            .HasForeignKey(p => p.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // One active price per (Customer, Product) — filtered to ignore soft-deleted rows.
        builder.HasIndex(p => new { p.CustomerId, p.ProductId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.Property(p => p.RowVersion).IsRowVersion();
    }
}
