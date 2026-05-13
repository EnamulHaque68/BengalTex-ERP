using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UomEntity = BengalTex.ERP.Domain.Entities.UnitOfMeasure;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.UnitOfMeasure;

public class UnitOfMeasureConfiguration : IEntityTypeConfiguration<UomEntity>
{
    public void Configure(EntityTypeBuilder<UomEntity> builder)
    {
        builder.ToTable("UnitsOfMeasure");

        builder.Property(u => u.Code).IsRequired().HasMaxLength(10);
        builder.Property(u => u.Name).IsRequired().HasMaxLength(50);
        builder.Property(u => u.Symbol).IsRequired().HasMaxLength(10);
        builder.Property(u => u.ConversionFactor).HasPrecision(18, 6);

        builder.Property(u => u.UnitType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(u => u.Code).IsUnique();
        builder.HasIndex(u => u.UnitType);
        builder.HasIndex(u => u.IsActive);

        // Self-reference for unit conversion (e.g., 1 DZN -> 12 PCS)
        builder.HasOne(u => u.BaseUnit)
            .WithMany()
            .HasForeignKey(u => u.BaseUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(u => u.RowVersion).IsRowVersion();
    }
}
