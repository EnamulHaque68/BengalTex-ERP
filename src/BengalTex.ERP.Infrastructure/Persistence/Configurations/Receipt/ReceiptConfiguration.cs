using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReceiptEntity = BengalTex.ERP.Domain.Entities.Receipt;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Receipt;

public class ReceiptConfiguration : IEntityTypeConfiguration<ReceiptEntity>
{
    public void Configure(EntityTypeBuilder<ReceiptEntity> builder)
    {
        builder.ToTable("Receipts");

        builder.Property(r => r.Code).IsRequired().HasMaxLength(50);
        builder.Property(r => r.Amount).HasPrecision(18, 4);
        builder.Property(r => r.ExchangeRate).HasPrecision(18, 6);
        builder.Property(r => r.BankChargeAmount).HasPrecision(18, 2);   // Phase A6b — FDBP
        builder.Property(r => r.InterestAmount).HasPrecision(18, 2);
        builder.Property(r => r.ReferenceNumber).HasMaxLength(100);
        builder.Property(r => r.Notes).HasMaxLength(2000);
        builder.Property(r => r.PostedBy).HasMaxLength(100);

        builder.Property(r => r.PaymentMethod)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(r => r.Code).IsUnique();
        builder.HasIndex(r => r.CustomerInvoiceId);
        builder.HasIndex(r => r.ReceiptDate);
        builder.HasIndex(r => r.PaymentMethod);
        builder.HasIndex(r => r.Status);

        // CustomerInvoice FK — Restrict so an invoice with receipts can't be deleted
        builder.HasOne(r => r.CustomerInvoice)
            .WithMany()
            .HasForeignKey(r => r.CustomerInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.RowVersion).IsRowVersion();
    }
}
