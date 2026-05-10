using BengalTex.ERP.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Identity;

public class SuspiciousLoginAttemptConfiguration : IEntityTypeConfiguration<SuspiciousLoginAttempt>
{
    public void Configure(EntityTypeBuilder<SuspiciousLoginAttempt> builder)
    {
        builder.ToTable("SuspiciousLoginAttempts", "identity");

        builder.Property(s => s.AttemptedUserName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.DeviceFingerprint)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.UserAgent).HasMaxLength(500);
        builder.Property(s => s.IpAddress).HasMaxLength(50);
        builder.Property(s => s.OperatingSystem).HasMaxLength(100);
        builder.Property(s => s.Reason)
            .HasMaxLength(200)
            .IsRequired();

        // Indexes for admin dashboard queries
        builder.HasIndex(s => s.AttemptedUserName);
        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.AttemptedAt);
        builder.HasIndex(s => s.AdminNotified);
    }
}