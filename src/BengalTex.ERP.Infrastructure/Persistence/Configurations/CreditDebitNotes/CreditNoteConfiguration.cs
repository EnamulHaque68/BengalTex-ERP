using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.CreditDebitNotes;

public class CreditNoteConfiguration : IEntityTypeConfiguration<CreditNote>
{
    public void Configure(EntityTypeBuilder<CreditNote> builder)
    {
        builder.ToTable("CreditNotes");

        builder.Property(n => n.Code).IsRequired().HasMaxLength(50);
        builder.Property(n => n.IssuedBy).HasMaxLength(100);
        builder.Property(n => n.Notes).HasMaxLength(2000);

        builder.Property(n => n.Amount).HasPrecision(18, 4);
        builder.Property(n => n.ExchangeRate).HasPrecision(18, 6);

        builder.Property(n => n.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(n => n.Reason).HasConversion<string>().HasMaxLength(30);

        builder.HasIndex(n => n.Code).IsUnique();
        builder.HasIndex(n => n.CustomerId);
        builder.HasIndex(n => n.CustomerInvoiceId);
        builder.HasIndex(n => n.Status);
        builder.HasIndex(n => n.IssueDate);

        builder.HasOne(n => n.Customer).WithMany()
            .HasForeignKey(n => n.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.CustomerInvoice).WithMany()
            .HasForeignKey(n => n.CustomerInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.Currency).WithMany()
            .HasForeignKey(n => n.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(n => n.CustomerReturnNoteId);
        builder.HasOne(n => n.CustomerReturnNote).WithMany()
            .HasForeignKey(n => n.CustomerReturnNoteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(n => n.RowVersion).IsRowVersion();
    }
}
