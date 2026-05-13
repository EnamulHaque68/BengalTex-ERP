using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CustomerEntity = BengalTex.ERP.Domain.Entities.Customer;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Customer;

public class CustomerConfiguration : IEntityTypeConfiguration<CustomerEntity>
{
    public void Configure(EntityTypeBuilder<CustomerEntity> builder)
    {
        builder.ToTable("Customers");

        builder.Property(c => c.Code).IsRequired().HasMaxLength(50);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.ContactPerson).HasMaxLength(200);
        builder.Property(c => c.Phone).HasMaxLength(30);
        builder.Property(c => c.Email).HasMaxLength(200);
        builder.Property(c => c.Website).HasMaxLength(200);

        builder.Property(c => c.AddressLine1).IsRequired().HasMaxLength(300);
        builder.Property(c => c.AddressLine2).HasMaxLength(300);
        builder.Property(c => c.City).IsRequired().HasMaxLength(100);
        builder.Property(c => c.District).HasMaxLength(100);
        builder.Property(c => c.PostalCode).HasMaxLength(20);
        builder.Property(c => c.Country).IsRequired().HasMaxLength(100).HasDefaultValue("Bangladesh");

        builder.Property(c => c.BinNumber).HasMaxLength(50);
        builder.Property(c => c.VatNumber).HasMaxLength(50);
        builder.Property(c => c.TinNumber).HasMaxLength(50);

        builder.Property(c => c.Category).HasConversion<string>().HasMaxLength(5);
        builder.Property(c => c.CreditLimit).HasPrecision(18, 2);

        builder.Property(c => c.Notes).HasMaxLength(2000);

        // Code is system-wide unique (when soft-deleted, the filtered index ignores it)
        builder.HasIndex(c => c.Code).IsUnique();
        builder.HasIndex(c => c.Name);
        builder.HasIndex(c => c.Category);
        builder.HasIndex(c => c.IsActive);

        builder.Property(c => c.RowVersion).IsRowVersion();
    }
}
