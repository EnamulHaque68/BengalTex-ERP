using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CompanyEntity = BengalTex.ERP.Domain.Entities.Company;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Company;

public class CompanyConfiguration : IEntityTypeConfiguration<CompanyEntity>
{
    public void Configure(EntityTypeBuilder<CompanyEntity> builder)
    {
        builder.ToTable("Companies");

        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.ShortName).HasMaxLength(50).IsRequired();
        builder.Property(c => c.RegistrationNumber).HasMaxLength(100);
        builder.Property(c => c.TaxNumber).HasMaxLength(50);

        builder.Property(c => c.AddressLine1).HasMaxLength(300).IsRequired();
        builder.Property(c => c.AddressLine2).HasMaxLength(300);
        builder.Property(c => c.City).HasMaxLength(100).IsRequired();
        builder.Property(c => c.District).HasMaxLength(100).IsRequired();
        builder.Property(c => c.PostalCode).HasMaxLength(20);
        builder.Property(c => c.Country).HasMaxLength(100).IsRequired().HasDefaultValue("Bangladesh");

        builder.Property(c => c.Phone).HasMaxLength(30);
        builder.Property(c => c.Email).HasMaxLength(200);
        builder.Property(c => c.Website).HasMaxLength(200);
        builder.Property(c => c.LogoUrl).HasMaxLength(500);

        builder.Property(c => c.RowVersion).IsRowVersion();
    }
}
