using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CustomerInvoiceEntity = BengalTex.ERP.Domain.Entities.CustomerInvoice;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.CustomerInvoice;

public class CustomerInvoiceConfiguration : IEntityTypeConfiguration<CustomerInvoiceEntity>
{
    public void Configure(EntityTypeBuilder<CustomerInvoiceEntity> builder)
    {
        builder.ToTable("CustomerInvoices");

        builder.Property(c => c.Code).IsRequired().HasMaxLength(50);
        builder.Property(c => c.IssuedBy).HasMaxLength(100);
        builder.Property(c => c.Notes).HasMaxLength(2000);

        // BD export-reporting fields (EPB / Form-N / Form-EXP)
        builder.Property(c => c.EpbFormNumber).HasMaxLength(50);
        builder.Property(c => c.LcNumber).HasMaxLength(50);
        builder.HasIndex(c => c.EpbFormNumber);
        builder.HasIndex(c => c.ShipmentDate);

        // Export shipping document fields (Commercial Invoice / Packing List)
        builder.Property(c => c.IncoTerm).HasMaxLength(20);
        builder.Property(c => c.PortOfLoading).HasMaxLength(100);
        builder.Property(c => c.PortOfDischarge).HasMaxLength(100);
        builder.Property(c => c.VesselName).HasMaxLength(100);
        builder.Property(c => c.CountryOfDestination).HasMaxLength(100);
        builder.Property(c => c.ShippingMarks).HasMaxLength(1000);
        builder.Property(c => c.GrossWeightKg).HasPrecision(12, 3);
        builder.Property(c => c.NetWeightKg).HasPrecision(12, 3);
        builder.Property(c => c.ContainerNumber).HasMaxLength(50);
        builder.Property(c => c.SealNumber).HasMaxLength(50);
        builder.Property(c => c.TruckNumber).HasMaxLength(50);
        builder.Property(c => c.VatRate).HasPrecision(7, 4);
        builder.Property(c => c.SubtotalAmount).HasPrecision(18, 4);
        builder.Property(c => c.VatAmount).HasPrecision(18, 4);
        builder.Property(c => c.TotalAmount).HasPrecision(18, 4);
        builder.Property(c => c.AmountPaid).HasPrecision(18, 4);

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(c => c.Code).IsUnique();
        builder.HasIndex(c => c.CustomerId);
        builder.HasIndex(c => c.SalesOrderId);
        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => c.InvoiceDate);
        builder.HasIndex(c => c.DueDate);

        // Customer FK — Restrict so a customer with invoices can't be deleted
        builder.HasOne(c => c.Customer)
            .WithMany()
            .HasForeignKey(c => c.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Transaction currency (Phase 21)
        builder.Property(c => c.ExchangeRate).HasPrecision(18, 6);
        builder.HasOne(c => c.Currency)
            .WithMany()
            .HasForeignKey(c => c.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        // SO FK — Restrict (invoice is derived from the SO; preserve linkage)
        builder.HasOne(c => c.SalesOrder)
            .WithMany()
            .HasForeignKey(c => c.SalesOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        // One-to-many: Invoice → Lines (cascade only triggers on hard delete; soft-delete leaves lines)
        builder.HasMany(c => c.Lines)
            .WithOne(l => l.CustomerInvoice)
            .HasForeignKey(l => l.CustomerInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(c => c.RowVersion).IsRowVersion();
    }
}

public class CustomerInvoiceLineConfiguration : IEntityTypeConfiguration<CustomerInvoiceLine>
{
    public void Configure(EntityTypeBuilder<CustomerInvoiceLine> builder)
    {
        builder.ToTable("CustomerInvoiceLines");

        builder.Property(l => l.Quantity).HasPrecision(18, 4);
        builder.Property(l => l.UnitPrice).HasPrecision(18, 4);
        builder.Property(l => l.LineNotes).HasMaxLength(1000);

        // Per-line export packing breakdown
        builder.Property(l => l.NetWeightKgPerLine).HasPrecision(12, 3);
        builder.Property(l => l.GrossWeightKgPerLine).HasPrecision(12, 3);

        builder.HasIndex(l => l.CustomerInvoiceId);
        builder.HasIndex(l => l.ProductId);

        // Product FK — Restrict so a product referenced by an invoice line can't be deleted
        builder.HasOne(l => l.Product)
            .WithMany()
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(l => l.RowVersion).IsRowVersion();
    }
}
