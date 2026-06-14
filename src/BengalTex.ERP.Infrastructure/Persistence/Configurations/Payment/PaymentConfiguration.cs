using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentEntity = BengalTex.ERP.Domain.Entities.Payment;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Payment;

public class PaymentConfiguration : IEntityTypeConfiguration<PaymentEntity>
{
    public void Configure(EntityTypeBuilder<PaymentEntity> builder)
    {
        builder.ToTable("Payments");

        builder.Property(p => p.Code).IsRequired().HasMaxLength(50);
        builder.Property(p => p.Amount).HasPrecision(18, 4);
        builder.Property(p => p.ExchangeRate).HasPrecision(18, 6);
        builder.Property(p => p.ReferenceNumber).HasMaxLength(100);
        builder.Property(p => p.Notes).HasMaxLength(2000);

        builder.Property(p => p.PaymentMethod)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(p => p.Code).IsUnique();
        builder.HasIndex(p => p.SupplierInvoiceId);
        builder.HasIndex(p => p.PaymentDate);
        builder.HasIndex(p => p.PaymentMethod);

        builder.HasOne(p => p.SupplierInvoice)
            .WithMany()
            .HasForeignKey(p => p.SupplierInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(p => p.RowVersion).IsRowVersion();
    }
}
