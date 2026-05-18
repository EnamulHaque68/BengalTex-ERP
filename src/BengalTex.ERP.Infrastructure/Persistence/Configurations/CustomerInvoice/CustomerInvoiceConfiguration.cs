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
