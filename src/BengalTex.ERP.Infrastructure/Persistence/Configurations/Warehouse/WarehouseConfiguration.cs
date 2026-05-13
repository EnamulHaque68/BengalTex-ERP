using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseEntity = BengalTex.ERP.Domain.Entities.Warehouse;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Warehouse;

public class WarehouseConfiguration : IEntityTypeConfiguration<WarehouseEntity>
{
    public void Configure(EntityTypeBuilder<WarehouseEntity> builder)
    {
        builder.ToTable("Warehouses");

        builder.Property(w => w.Code).IsRequired().HasMaxLength(20);
        builder.Property(w => w.Name).IsRequired().HasMaxLength(100);
        builder.Property(w => w.Address).HasMaxLength(300);

        builder.Property(w => w.WarehouseType)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Same code can repeat across factories, but unique within a factory
        builder.HasIndex(w => new { w.FactoryId, w.Code }).IsUnique();
        builder.HasIndex(w => w.IsActive);
        builder.HasIndex(w => w.WarehouseType);

        builder.HasOne(w => w.Factory)
            .WithMany()
            .HasForeignKey(w => w.FactoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(w => w.RowVersion).IsRowVersion();
    }
}
