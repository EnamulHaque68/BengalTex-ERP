using BengalTex.ERP.Infrastructure.Persistence.CrossCutting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.CrossCutting;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.Property(n => n.Channel).IsRequired().HasMaxLength(20);
        builder.Property(n => n.Recipient).IsRequired().HasMaxLength(300);
        builder.Property(n => n.Subject).IsRequired().HasMaxLength(300);
        builder.Property(n => n.Body).HasColumnType("nvarchar(max)");
        builder.Property(n => n.RelatedEntityType).HasMaxLength(100);
        builder.Property(n => n.Status).IsRequired().HasMaxLength(20);
        builder.Property(n => n.Error).HasMaxLength(2000);

        builder.Property(n => n.RowVersion).IsRowVersion();

        builder.HasIndex(n => n.Channel);
        builder.HasIndex(n => n.Status);
        builder.HasIndex(n => new { n.RelatedEntityType, n.RelatedEntityId });
    }
}
