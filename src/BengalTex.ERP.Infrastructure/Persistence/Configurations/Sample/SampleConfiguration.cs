using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Sample;

public class SampleConfiguration : IEntityTypeConfiguration<Domain.Entities.Sample>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Sample> builder)
    {
        builder.ToTable("Samples");

        builder.Property(s => s.Code).IsRequired().HasMaxLength(50);
        builder.Property(s => s.Title).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Description).HasMaxLength(2000);
        builder.Property(s => s.BuyerReference).HasMaxLength(100);
        builder.Property(s => s.Feedback).HasMaxLength(2000);
        builder.Property(s => s.Notes).HasMaxLength(2000);
        builder.Property(s => s.DecidedBy).HasMaxLength(100);
        builder.Property(s => s.Quantity).HasPrecision(18, 4);

        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(s => s.Code).IsUnique();
        builder.HasIndex(s => s.CustomerId);
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.RequestedDate);

        builder.HasOne(s => s.Customer)
            .WithMany().HasForeignKey(s => s.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Product)
            .WithMany().HasForeignKey(s => s.ProductId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(s => s.Style)
            .WithMany().HasForeignKey(s => s.StyleId).OnDelete(DeleteBehavior.SetNull);

        builder.Property(s => s.RowVersion).IsRowVersion();
    }
}
