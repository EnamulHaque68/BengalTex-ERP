using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.GoodsReceipt;

public class GoodsReceiptNoteConfiguration : IEntityTypeConfiguration<GoodsReceiptNote>
{
    public void Configure(EntityTypeBuilder<GoodsReceiptNote> builder)
    {
        builder.ToTable("GoodsReceiptNotes");

        builder.Property(g => g.Code).IsRequired().HasMaxLength(50);
        builder.Property(g => g.SupplierDeliveryRef).HasMaxLength(100);
        builder.Property(g => g.PostedBy).HasMaxLength(100);
        builder.Property(g => g.Notes).HasMaxLength(2000);

        builder.Property(g => g.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(g => g.Code).IsUnique();
        builder.HasIndex(g => g.PurchaseOrderId);
        builder.HasIndex(g => g.LetterOfCreditId);
        builder.HasIndex(g => g.Status);
        builder.HasIndex(g => g.ReceiveDate);

        // PO FK — Restrict so a PO with GRNs can't be deleted out from under them
        builder.HasOne(g => g.PurchaseOrder)
            .WithMany()
            .HasForeignKey(g => g.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        // Optional LC link (import purchases) — Restrict so an LC with GRNs can't be deleted.
        builder.HasOne(g => g.LetterOfCredit)
            .WithMany()
            .HasForeignKey(g => g.LetterOfCreditId)
            .OnDelete(DeleteBehavior.Restrict);

        // Receiving warehouse FK — Restrict (required field)
        builder.HasOne(g => g.ReceivingWarehouse)
            .WithMany()
            .HasForeignKey(g => g.ReceivingWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        // One-to-many: GRN → Lines
        builder.HasMany(g => g.Lines)
            .WithOne(l => l.GoodsReceiptNote)
            .HasForeignKey(l => l.GoodsReceiptNoteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(g => g.RowVersion).IsRowVersion();
    }
}

public class GoodsReceiptLineConfiguration : IEntityTypeConfiguration<GoodsReceiptLine>
{
    public void Configure(EntityTypeBuilder<GoodsReceiptLine> builder)
    {
        builder.ToTable("GoodsReceiptLines");

        builder.Property(l => l.ReceivedQuantity).HasPrecision(18, 4);
        builder.Property(l => l.ReturnedQuantity).HasPrecision(18, 4);
        builder.Property(l => l.LineNotes).HasMaxLength(1000);

        // Optional lot/batch capture (creates a StockLot on GRN post)
        builder.Property(l => l.LotNumber).HasMaxLength(100);
        builder.Property(l => l.Shade).HasMaxLength(100);

        builder.HasIndex(l => l.GoodsReceiptNoteId);
        builder.HasIndex(l => l.PurchaseOrderLineId);

        // PO line FK — Restrict so the originating PO line can be traced back
        builder.HasOne(l => l.PurchaseOrderLine)
            .WithMany()
            .HasForeignKey(l => l.PurchaseOrderLineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(l => l.RowVersion).IsRowVersion();
    }
}
