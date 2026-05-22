using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Banking;

public class LetterOfCreditConfiguration : IEntityTypeConfiguration<LetterOfCredit>
{
    public void Configure(EntityTypeBuilder<LetterOfCredit> builder)
    {
        builder.ToTable("LettersOfCredit");

        builder.Property(l => l.Code).IsRequired().HasMaxLength(50);
        builder.Property(l => l.LcNumber).IsRequired().HasMaxLength(100);
        builder.Property(l => l.IssuingBank).IsRequired().HasMaxLength(200);
        builder.Property(l => l.Amount).HasPrecision(18, 2);
        builder.Property(l => l.ExchangeRate).HasPrecision(18, 6);
        builder.Property(l => l.Notes).HasMaxLength(2000);

        builder.Property(l => l.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(l => l.Code).IsUnique();
        builder.HasIndex(l => l.SupplierId);
        builder.HasIndex(l => l.Status);
        builder.HasIndex(l => l.IssueDate);

        builder.HasOne(l => l.Supplier)
            .WithMany()
            .HasForeignKey(l => l.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.PurchaseOrder)
            .WithMany()
            .HasForeignKey(l => l.PurchaseOrderId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(l => l.Currency)
            .WithMany()
            .HasForeignKey(l => l.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(l => l.RowVersion).IsRowVersion();
    }
}
