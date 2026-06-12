using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.CreditDebitNotes;

public class DebitNoteConfiguration : IEntityTypeConfiguration<DebitNote>
{
    public void Configure(EntityTypeBuilder<DebitNote> builder)
    {
        builder.ToTable("DebitNotes");

        builder.Property(n => n.Code).IsRequired().HasMaxLength(50);
        builder.Property(n => n.IssuedBy).HasMaxLength(100);
        builder.Property(n => n.Notes).HasMaxLength(2000);

        builder.Property(n => n.Amount).HasPrecision(18, 4);
        builder.Property(n => n.ExchangeRate).HasPrecision(18, 6);

        builder.Property(n => n.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(n => n.Reason).HasConversion<string>().HasMaxLength(30);

        builder.HasIndex(n => n.Code).IsUnique();
        builder.HasIndex(n => n.SupplierId);
        builder.HasIndex(n => n.SupplierInvoiceId);
        builder.HasIndex(n => n.Status);
        builder.HasIndex(n => n.IssueDate);

        builder.HasOne(n => n.Supplier).WithMany()
            .HasForeignKey(n => n.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.SupplierInvoice).WithMany()
            .HasForeignKey(n => n.SupplierInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.Currency).WithMany()
            .HasForeignKey(n => n.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(n => n.SupplierReturnNoteId);
        builder.HasOne(n => n.SupplierReturnNote).WithMany()
            .HasForeignKey(n => n.SupplierReturnNoteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(n => n.RowVersion).IsRowVersion();
    }
}
