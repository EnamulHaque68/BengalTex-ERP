using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Production;

public class MachineConfiguration : IEntityTypeConfiguration<Machine>
{
    public void Configure(EntityTypeBuilder<Machine> builder)
    {
        builder.ToTable("Machines");

        builder.Property(m => m.Code).IsRequired().HasMaxLength(50);
        builder.Property(m => m.Name).IsRequired().HasMaxLength(200);
        builder.Property(m => m.MachineType).HasMaxLength(80);
        builder.Property(m => m.Location).HasMaxLength(150);
        builder.Property(m => m.CapacityPerHour).HasPrecision(12, 2);
        builder.Property(m => m.Notes).HasMaxLength(1000);

        builder.HasIndex(m => m.Code).IsUnique();
        builder.HasIndex(m => m.MachineType);
        builder.HasIndex(m => m.IsActive);

        builder.Property(m => m.RowVersion).IsRowVersion();
    }
}
