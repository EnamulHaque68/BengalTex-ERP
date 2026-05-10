using BengalTex.ERP.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Identity;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(u => u.BoundDeviceFingerprint).HasMaxLength(200);
        builder.Property(u => u.BoundDeviceName).HasMaxLength(200);
        builder.Property(u => u.CurrentRefreshTokenHash).HasMaxLength(500);
        builder.Property(u => u.CurrentSessionId).HasMaxLength(100);

        // Indexes for fast lookups
        builder.HasIndex(u => u.FactoryId);
        builder.HasIndex(u => u.IsActive);
        builder.HasIndex(u => u.BoundDeviceFingerprint);

        // One-to-many: User → DeviceHistory
        builder.HasMany(u => u.DeviceHistory)
            .WithOne()
            .HasForeignKey(h => h.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}