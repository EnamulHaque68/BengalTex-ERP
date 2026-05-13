using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FactoryEntity = BengalTex.ERP.Domain.Entities.Factory;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Factory;

public class FactoryConfiguration : IEntityTypeConfiguration<FactoryEntity>
{
    public void Configure(EntityTypeBuilder<FactoryEntity> builder)
    {
        builder.ToTable("Factories");

        builder.Property(f => f.Name).HasMaxLength(200).IsRequired();
        builder.Property(f => f.Code).HasMaxLength(20).IsRequired();

        builder.Property(f => f.AddressLine1).HasMaxLength(300).IsRequired();
        builder.Property(f => f.AddressLine2).HasMaxLength(300);
        builder.Property(f => f.City).HasMaxLength(100).IsRequired();
        builder.Property(f => f.District).HasMaxLength(100);
        builder.Property(f => f.PostalCode).HasMaxLength(20);
        builder.Property(f => f.Phone).HasMaxLength(30);

        builder.Property(f => f.GeoFenceLat).HasColumnType("float");
        builder.Property(f => f.GeoFenceLng).HasColumnType("float");
        builder.Property(f => f.GeoFenceRadiusMeters).HasColumnType("float");

        builder.HasIndex(f => f.Code).IsUnique();

        builder.HasOne(f => f.Company)
            .WithMany(c => c.Factories)
            .HasForeignKey(f => f.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(f => f.RowVersion).IsRowVersion();
    }
}
