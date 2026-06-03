using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProformaInvoiceEntity = BengalTex.ERP.Domain.Entities.ProformaInvoice;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.ProformaInvoice;

public class ProformaInvoiceConfiguration : IEntityTypeConfiguration<ProformaInvoiceEntity>
{
    public void Configure(EntityTypeBuilder<ProformaInvoiceEntity> builder)
    {
        builder.ToTable("ProformaInvoices");

        builder.Property(p => p.Code).IsRequired().HasMaxLength(50);
        builder.Property(p => p.SentBy).HasMaxLength(100);
        builder.Property(p => p.Notes).HasMaxLength(2000);

        builder.Property(p => p.VatRate).HasPrecision(7, 4);
        builder.Property(p => p.SubtotalAmount).HasPrecision(18, 4);
        builder.Property(p => p.VatAmount).HasPrecision(18, 4);
        builder.Property(p => p.TotalAmount).HasPrecision(18, 4);
        builder.Property(p => p.ExchangeRate).HasPrecision(18, 6);

        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(p => p.Code).IsUnique();
        builder.HasIndex(p => p.CustomerId);
        builder.HasIndex(p => p.SalesOrderId);
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.IssueDate);

        builder.HasOne(p => p.Customer).WithMany()
            .HasForeignKey(p => p.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.SalesOrder).WithMany()
            .HasForeignKey(p => p.SalesOrderId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(p => p.Currency).WithMany()
            .HasForeignKey(p => p.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        // 1-to-1: a Proforma points to the eventual real invoice (nullable until conversion)
        builder.HasOne(p => p.ConvertedCustomerInvoice).WithMany()
            .HasForeignKey(p => p.ConvertedCustomerInvoiceId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(p => p.Lines)
            .WithOne(l => l.ProformaInvoice)
            .HasForeignKey(l => l.ProformaInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(p => p.RowVersion).IsRowVersion();
    }
}

public class ProformaInvoiceLineConfiguration : IEntityTypeConfiguration<ProformaInvoiceLine>
{
    public void Configure(EntityTypeBuilder<ProformaInvoiceLine> builder)
    {
        builder.ToTable("ProformaInvoiceLines");

        builder.Property(l => l.Quantity).HasPrecision(18, 4);
        builder.Property(l => l.UnitPrice).HasPrecision(18, 4);
        builder.Property(l => l.LineNotes).HasMaxLength(1000);

        builder.HasIndex(l => l.ProformaInvoiceId);
        builder.HasIndex(l => l.ProductId);

        builder.HasOne(l => l.Product).WithMany()
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(l => l.RowVersion).IsRowVersion();
    }
}
