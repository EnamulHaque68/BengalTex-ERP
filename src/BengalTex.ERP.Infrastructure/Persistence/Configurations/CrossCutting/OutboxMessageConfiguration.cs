using BengalTex.ERP.Infrastructure.Persistence.CrossCutting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.CrossCutting;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.Property(o => o.Type)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(o => o.Payload)
            .HasColumnType("nvarchar(max)")     // JSON can be large
            .IsRequired();

        builder.Property(o => o.Error)
            .HasColumnType("nvarchar(max)");

        // CRITICAL index: Hangfire job processes unprocessed messages
        builder.HasIndex(o => new { o.ProcessedOn, o.OccurredOn })
            .HasDatabaseName("IX_OutboxMessages_Pending");
    }
}