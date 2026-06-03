using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.GatePass;

public class GatePassConfiguration : IEntityTypeConfiguration<Domain.Entities.GatePass>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.GatePass> builder)
    {
        builder.ToTable("GatePasses");

        builder.Property(g => g.Code).IsRequired().HasMaxLength(50);
        builder.Property(g => g.VehicleNumber).HasMaxLength(30);
        builder.Property(g => g.DriverName).HasMaxLength(100);
        builder.Property(g => g.DriverPhone).HasMaxLength(30);
        builder.Property(g => g.DriverNidNumber).HasMaxLength(30);
        builder.Property(g => g.TransporterName).HasMaxLength(150);
        builder.Property(g => g.VisitorName).HasMaxLength(100);
        builder.Property(g => g.VisitorPhone).HasMaxLength(30);
        builder.Property(g => g.VisitorOrganization).HasMaxLength(150);
        builder.Property(g => g.VisitorPurpose).HasMaxLength(500);
        builder.Property(g => g.ItemDescription).HasMaxLength(1000);
        builder.Property(g => g.Quantity).HasMaxLength(100);
        builder.Property(g => g.FromLocation).HasMaxLength(150);
        builder.Property(g => g.ToLocation).HasMaxLength(150);
        builder.Property(g => g.SourceType).HasMaxLength(50);
        builder.Property(g => g.SourceCode).HasMaxLength(100);
        builder.Property(g => g.IssuedByUser).HasMaxLength(100);
        builder.Property(g => g.ApprovedByUser).HasMaxLength(100);
        builder.Property(g => g.ReturnedByUser).HasMaxLength(100);
        builder.Property(g => g.ReturnNotes).HasMaxLength(1000);
        builder.Property(g => g.Notes).HasMaxLength(2000);

        builder.Property(g => g.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(g => g.Direction).HasConversion<string>().HasMaxLength(10);
        builder.Property(g => g.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(g => g.Code).IsUnique();
        builder.HasIndex(g => g.PassDate);
        builder.HasIndex(g => g.Status);
        builder.HasIndex(g => g.Type);
        builder.HasIndex(g => g.VehicleNumber);
        builder.HasIndex(g => new { g.SourceType, g.SourceId });

        builder.Property(g => g.RowVersion).IsRowVersion();
    }
}
