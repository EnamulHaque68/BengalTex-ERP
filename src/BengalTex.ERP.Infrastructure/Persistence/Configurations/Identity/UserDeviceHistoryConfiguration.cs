using BengalTex.ERP.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Identity;

public class UserDeviceHistoryConfiguration : IEntityTypeConfiguration<UserDeviceHistory>
{
    public void Configure(EntityTypeBuilder<UserDeviceHistory> builder)
    {
        builder.ToTable("UserDeviceHistory", "identity");

        builder.Property(h => h.DeviceFingerprint)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(h => h.DeviceName).HasMaxLength(200);
        builder.Property(h => h.UserAgent).HasMaxLength(500);
        builder.Property(h => h.IpAddress).HasMaxLength(50);
        builder.Property(h => h.OperatingSystem).HasMaxLength(100);
        builder.Property(h => h.BrowserInfo).HasMaxLength(200);
        builder.Property(h => h.UnbindReason).HasMaxLength(500);
        builder.Property(h => h.UnboundBy).HasMaxLength(100);

        // Convert enum to string in DB (readable)
        builder.Property(h => h.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Indexes
        builder.HasIndex(h => h.UserId);
        builder.HasIndex(h => h.DeviceFingerprint);
        builder.HasIndex(h => new { h.UserId, h.Status });  // Composite
    }
}