using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupplierEntity = BengalTex.ERP.Domain.Entities.Supplier;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Supplier;

public class SupplierConfiguration : IEntityTypeConfiguration<SupplierEntity>
{
    public void Configure(EntityTypeBuilder<SupplierEntity> builder)
    {
        builder.ToTable("Suppliers");

        builder.Property(s => s.Code).IsRequired().HasMaxLength(50);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.ContactPerson).HasMaxLength(200);
        builder.Property(s => s.Phone).HasMaxLength(30);
        builder.Property(s => s.Email).HasMaxLength(200);
        builder.Property(s => s.Website).HasMaxLength(200);

        builder.Property(s => s.AddressLine1).IsRequired().HasMaxLength(300);
        builder.Property(s => s.AddressLine2).HasMaxLength(300);
        builder.Property(s => s.City).IsRequired().HasMaxLength(100);
        builder.Property(s => s.District).HasMaxLength(100);
        builder.Property(s => s.PostalCode).HasMaxLength(20);
        builder.Property(s => s.Country).IsRequired().HasMaxLength(100).HasDefaultValue("Bangladesh");

        builder.Property(s => s.BinNumber).HasMaxLength(50);
        builder.Property(s => s.VatNumber).HasMaxLength(50);
        builder.Property(s => s.TinNumber).HasMaxLength(50);

        builder.Property(s => s.BankName).HasMaxLength(100);
        builder.Property(s => s.BankAccountNumber).HasMaxLength(50);
        builder.Property(s => s.BankBranch).HasMaxLength(100);
        builder.Property(s => s.BankAccountHolderName).HasMaxLength(200);

        builder.Property(s => s.Notes).HasMaxLength(2000);

        builder.HasIndex(s => s.Code).IsUnique();
        builder.HasIndex(s => s.Name);
        builder.HasIndex(s => s.IsActive);

        builder.Property(s => s.RowVersion).IsRowVersion();
    }
}
