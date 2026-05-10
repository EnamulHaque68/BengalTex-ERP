using BengalTex.ERP.Infrastructure.Persistence.CrossCutting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.CrossCutting;

public class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("AuditLogEntries");

        builder.Property(a => a.EntityType)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(a => a.EntityKey)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.Action)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.UserId).HasMaxLength(100);
        builder.Property(a => a.UserName).HasMaxLength(200);
        builder.Property(a => a.IpAddress).HasMaxLength(50);
        builder.Property(a => a.UserAgent).HasMaxLength(500);

        // JSON columns can be large
        builder.Property(a => a.OldValuesJson).HasColumnType("nvarchar(max)");
        builder.Property(a => a.NewValuesJson).HasColumnType("nvarchar(max)");
        builder.Property(a => a.AffectedColumns).HasMaxLength(2000);

        // CRITICAL indexes for compliance queries
        builder.HasIndex(a => new { a.EntityType, a.EntityKey });
        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.Timestamp);
    }
}