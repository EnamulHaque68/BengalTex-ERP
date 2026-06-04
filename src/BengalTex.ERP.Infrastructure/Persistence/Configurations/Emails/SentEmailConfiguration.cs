using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Emails;

public class SentEmailConfiguration : IEntityTypeConfiguration<SentEmail>
{
    public void Configure(EntityTypeBuilder<SentEmail> builder)
    {
        builder.ToTable("SentEmails");

        builder.Property(e => e.SentByUser).IsRequired().HasMaxLength(100);
        builder.Property(e => e.SourceType).HasMaxLength(50);
        builder.Property(e => e.SourceCode).HasMaxLength(100);
        builder.Property(e => e.ToAddresses).IsRequired().HasMaxLength(1000);
        builder.Property(e => e.CcAddresses).HasMaxLength(1000);
        builder.Property(e => e.Subject).IsRequired().HasMaxLength(300);
        builder.Property(e => e.Body).IsRequired();   // nvarchar(max)
        builder.Property(e => e.ErrorMessage).HasMaxLength(2000);

        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(e => e.SentAt);
        builder.HasIndex(e => new { e.SourceType, e.SourceId });
        builder.HasIndex(e => e.Status);

        builder.Property(e => e.RowVersion).IsRowVersion();
    }
}
