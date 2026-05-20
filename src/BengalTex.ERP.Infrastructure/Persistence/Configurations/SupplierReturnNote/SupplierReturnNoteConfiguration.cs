using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.SupplierReturnNote;

public class SupplierReturnNoteConfiguration : IEntityTypeConfiguration<Domain.Entities.SupplierReturnNote>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.SupplierReturnNote> builder)
    {
        builder.ToTable("SupplierReturnNotes");

        builder.Property(s => s.Code).IsRequired().HasMaxLength(50);
        builder.Property(s => s.VehicleNumber).HasMaxLength(50);
        builder.Property(s => s.Reason).HasMaxLength(500);
        builder.Property(s => s.PostedBy).HasMaxLength(100);
        builder.Property(s => s.Notes).HasMaxLength(2000);

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(s => s.Code).IsUnique();
        builder.HasIndex(s => s.GoodsReceiptNoteId);
        builder.HasIndex(s => s.ReturnFromWarehouseId);
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.ReturnDate);

        builder.HasOne(s => s.GoodsReceiptNote)
            .WithMany()
            .HasForeignKey(s => s.GoodsReceiptNoteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.ReturnFromWarehouse)
            .WithMany()
            .HasForeignKey(s => s.ReturnFromWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Lines)
            .WithOne(l => l.SupplierReturnNote)
            .HasForeignKey(l => l.SupplierReturnNoteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(s => s.RowVersion).IsRowVersion();
    }
}

public class SupplierReturnNoteLineConfiguration : IEntityTypeConfiguration<SupplierReturnNoteLine>
{
    public void Configure(EntityTypeBuilder<SupplierReturnNoteLine> builder)
    {
        builder.ToTable("SupplierReturnNoteLines");

        builder.Property(l => l.ReturnedQuantity).HasPrecision(18, 4);
        builder.Property(l => l.LineNotes).HasMaxLength(1000);

        builder.HasIndex(l => l.SupplierReturnNoteId);
        builder.HasIndex(l => l.GoodsReceiptLineId);
        builder.HasIndex(l => l.RawMaterialId);

        builder.HasOne(l => l.GoodsReceiptLine)
            .WithMany()
            .HasForeignKey(l => l.GoodsReceiptLineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.RawMaterial)
            .WithMany()
            .HasForeignKey(l => l.RawMaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(l => l.RowVersion).IsRowVersion();
    }
}
