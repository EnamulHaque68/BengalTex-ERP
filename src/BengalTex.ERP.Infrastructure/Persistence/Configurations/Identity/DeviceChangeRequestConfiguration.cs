using BengalTex.ERP.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Identity;

public class DeviceChangeRequestConfiguration : IEntityTypeConfiguration<DeviceChangeRequest>
{
    public void Configure(EntityTypeBuilder<DeviceChangeRequest> builder)
    {
        builder.ToTable("DeviceChangeRequests", "identity");

        builder.Property(d => d.OldDeviceFingerprint)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(d => d.NewDeviceFingerprint)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(d => d.NewDeviceName).HasMaxLength(200);
        builder.Property(d => d.NewUserAgent).HasMaxLength(500);
        builder.Property(d => d.NewIpAddress).HasMaxLength(50);
        builder.Property(d => d.Reason)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(d => d.ReviewedBy).HasMaxLength(100);
        builder.Property(d => d.ReviewComment).HasMaxLength(1000);

        // Enum as string
        builder.Property(d => d.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Indexes for admin queries
        builder.HasIndex(d => d.UserId);
        builder.HasIndex(d => d.Status);
        builder.HasIndex(d => new { d.UserId, d.Status });
    }
}