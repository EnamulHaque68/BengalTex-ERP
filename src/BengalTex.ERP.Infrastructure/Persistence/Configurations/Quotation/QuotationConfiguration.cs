using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Quotation;

public class QuotationConfiguration : IEntityTypeConfiguration<Domain.Entities.Quotation>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Quotation> builder)
    {
        builder.ToTable("Quotations");

        builder.Property(q => q.Code).IsRequired().HasMaxLength(50);
        builder.Property(q => q.CustomerReference).HasMaxLength(100);
        builder.Property(q => q.Notes).HasMaxLength(2000);
        builder.Property(q => q.DecidedBy).HasMaxLength(100);

        builder.Property(q => q.ExchangeRate).HasPrecision(18, 6);
        builder.Property(q => q.TotalAmount).HasPrecision(18, 2);

        builder.Property(q => q.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(q => q.Code).IsUnique();
        builder.HasIndex(q => q.CustomerId);
        builder.HasIndex(q => q.Status);
        builder.HasIndex(q => q.QuotationDate);

        builder.HasOne(q => q.Customer)
            .WithMany().HasForeignKey(q => q.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(q => q.Currency)
            .WithMany().HasForeignKey(q => q.CurrencyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(q => q.RevisionOf)
            .WithMany().HasForeignKey(q => q.RevisionOfId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(q => q.Lines)
            .WithOne(l => l.Quotation)
            .HasForeignKey(l => l.QuotationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(q => q.RowVersion).IsRowVersion();
    }
}

public class QuotationLineConfiguration : IEntityTypeConfiguration<QuotationLine>
{
    public void Configure(EntityTypeBuilder<QuotationLine> builder)
    {
        builder.ToTable("QuotationLines");

        builder.Property(l => l.Description).HasMaxLength(500);

        foreach (var p in new[] { nameof(QuotationLine.Quantity), nameof(QuotationLine.MaterialCost),
                     nameof(QuotationLine.LaborCost), nameof(QuotationLine.MachineCost), nameof(QuotationLine.OverheadCost),
                     nameof(QuotationLine.WastagePercent), nameof(QuotationLine.MarginPercent),
                     nameof(QuotationLine.UnitCost), nameof(QuotationLine.UnitPrice), nameof(QuotationLine.LineTotal) })
            builder.Property(p).HasPrecision(18, 4);

        builder.HasIndex(l => l.QuotationId);

        builder.HasOne(l => l.Product)
            .WithMany().HasForeignKey(l => l.ProductId).OnDelete(DeleteBehavior.Restrict);

        builder.Property(l => l.RowVersion).IsRowVersion();
    }
}
