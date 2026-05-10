using BengalTex.ERP.Infrastructure.Persistence.CrossCutting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.CrossCutting;

public class DocumentAttachmentConfiguration : IEntityTypeConfiguration<DocumentAttachment>
{
    public void Configure(EntityTypeBuilder<DocumentAttachment> builder)
    {
        builder.ToTable("DocumentAttachments");

        builder.Property(d => d.EntityType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(d => d.FileName)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(d => d.StoredFileName)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(d => d.ContentType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(d => d.StoragePath)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(d => d.Description).HasMaxLength(500);
        builder.Property(d => d.Category).HasMaxLength(50);

        // CRITICAL composite index for polymorphic lookups
        builder.HasIndex(d => new { d.EntityType, d.EntityId })
            .HasDatabaseName("IX_DocumentAttachments_Entity");

        builder.HasIndex(d => d.Category);
    }
}