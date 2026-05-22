using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StyleEntity = BengalTex.ERP.Domain.Entities.Style;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Style;

public class StyleConfiguration : IEntityTypeConfiguration<StyleEntity>
{
    public void Configure(EntityTypeBuilder<StyleEntity> builder)
    {
        builder.ToTable("Styles");

        builder.Property(s => s.Code).IsRequired().HasMaxLength(50);
        builder.Property(s => s.StyleName).IsRequired().HasMaxLength(200);
        builder.Property(s => s.BuyerStyleRef).HasMaxLength(100);
        builder.Property(s => s.Season).HasMaxLength(50);
        builder.Property(s => s.Description).HasMaxLength(2000);
        builder.Property(s => s.Notes).HasMaxLength(2000);

        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(s => s.Code).IsUnique();
        builder.HasIndex(s => s.StyleName);
        builder.HasIndex(s => s.BuyerId);
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.IsActive);

        builder.HasOne(s => s.Buyer)
            .WithMany()
            .HasForeignKey(s => s.BuyerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Product)
            .WithMany()
            .HasForeignKey(s => s.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(s => s.RowVersion).IsRowVersion();
    }
}
