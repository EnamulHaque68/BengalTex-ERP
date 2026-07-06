using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupplierInvoiceEntity = BengalTex.ERP.Domain.Entities.SupplierInvoice;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.SupplierInvoice;

public class SupplierInvoiceConfiguration : IEntityTypeConfiguration<SupplierInvoiceEntity>
{
    public void Configure(EntityTypeBuilder<SupplierInvoiceEntity> builder)
    {
        builder.ToTable("SupplierInvoices");

        builder.Property(s => s.Code).IsRequired().HasMaxLength(50);
        builder.Property(s => s.SupplierInvoiceNumber).HasMaxLength(100);
        builder.Property(s => s.ApprovedBy).HasMaxLength(100);
        builder.Property(s => s.Notes).HasMaxLength(2000);
        builder.Property(s => s.VatRate).HasPrecision(7, 4);
        builder.Property(s => s.SubtotalAmount).HasPrecision(18, 4);
        builder.Property(s => s.VatAmount).HasPrecision(18, 4);
        builder.Property(s => s.TotalAmount).HasPrecision(18, 4);
        builder.Property(s => s.AmountPaid).HasPrecision(18, 4);

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(s => s.Code).IsUnique();
        builder.HasIndex(s => s.SupplierId);
        builder.HasIndex(s => s.PurchaseOrderId);
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.InvoiceDate);
        builder.HasIndex(s => s.DueDate);

        builder.HasOne(s => s.Supplier)
            .WithMany()
            .HasForeignKey(s => s.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        // Transaction currency (Phase 21)
        builder.Property(s => s.ExchangeRate).HasPrecision(18, 6);
        builder.HasOne(s => s.Currency)
            .WithMany()
            .HasForeignKey(s => s.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.PurchaseOrder)
            .WithMany()
            .HasForeignKey(s => s.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Lines)
            .WithOne(l => l.SupplierInvoice)
            .HasForeignKey(l => l.SupplierInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(s => s.RowVersion).IsRowVersion();
    }
}

public class SupplierInvoiceLineConfiguration : IEntityTypeConfiguration<SupplierInvoiceLine>
{
    public void Configure(EntityTypeBuilder<SupplierInvoiceLine> builder)
    {
        builder.ToTable("SupplierInvoiceLines", t =>
            // Phase A2 — a line is material (RawMaterialId) XOR service (AccountId), never both/neither.
            t.HasCheckConstraint("CK_SupplierInvoiceLine_ItemXorService",
                "([RawMaterialId] IS NOT NULL AND [AccountId] IS NULL) OR " +
                "([RawMaterialId] IS NULL AND [AccountId] IS NOT NULL)"));

        builder.Property(l => l.Quantity).HasPrecision(18, 4);
        builder.Property(l => l.UnitPrice).HasPrecision(18, 4);
        builder.Property(l => l.LineNotes).HasMaxLength(1000);

        builder.HasIndex(l => l.SupplierInvoiceId);
        builder.HasIndex(l => l.RawMaterialId);
        builder.HasIndex(l => l.AccountId);

        builder.HasOne(l => l.RawMaterial)
            .WithMany()
            .HasForeignKey(l => l.RawMaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        // Service-line expense account (Phase A2) — Restrict so a posted-to account can't be deleted.
        builder.HasOne(l => l.Account)
            .WithMany()
            .HasForeignKey(l => l.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(l => l.RowVersion).IsRowVersion();
    }
}
