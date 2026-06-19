using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Purchasing;

public class SupplierQuotationConfiguration : IEntityTypeConfiguration<SupplierQuotation>
{
    public void Configure(EntityTypeBuilder<SupplierQuotation> builder)
    {
        builder.ToTable("SupplierQuotations");

        builder.Property(q => q.Code).IsRequired().HasMaxLength(50);
        builder.Property(q => q.ExchangeRate).HasPrecision(18, 6);
        builder.Property(q => q.DecidedBy).HasMaxLength(100);
        builder.Property(q => q.Notes).HasMaxLength(2000);
        builder.Property(q => q.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(q => q.Code).IsUnique();
        builder.HasIndex(q => q.Status);
        builder.HasIndex(q => q.SupplierId);
        builder.HasIndex(q => q.PurchaseRequisitionId);

        builder.HasOne(q => q.Supplier)
            .WithMany()
            .HasForeignKey(q => q.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(q => q.Currency)
            .WithMany()
            .HasForeignKey(q => q.CurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Optional PR link — SetNull so deleting a requisition doesn't cascade-delete quotes
        builder.HasOne(q => q.PurchaseRequisition)
            .WithMany()
            .HasForeignKey(q => q.PurchaseRequisitionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(q => q.Lines)
            .WithOne(l => l.SupplierQuotation)
            .HasForeignKey(l => l.SupplierQuotationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(q => q.RowVersion).IsRowVersion();
    }
}

public class SupplierQuotationLineConfiguration : IEntityTypeConfiguration<SupplierQuotationLine>
{
    public void Configure(EntityTypeBuilder<SupplierQuotationLine> builder)
    {
        builder.ToTable("SupplierQuotationLines");

        builder.Property(l => l.Quantity).HasPrecision(18, 4);
        builder.Property(l => l.UnitPrice).HasPrecision(18, 4);
        builder.Property(l => l.LineNotes).HasMaxLength(1000);

        builder.HasIndex(l => l.SupplierQuotationId);
        builder.HasIndex(l => l.RawMaterialId);

        builder.HasOne(l => l.RawMaterial)
            .WithMany()
            .HasForeignKey(l => l.RawMaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(l => l.RowVersion).IsRowVersion();
    }
}
