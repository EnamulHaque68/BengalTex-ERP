using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockTransferEntity = BengalTex.ERP.Domain.Entities.StockTransfer;
using StockTransferLineEntity = BengalTex.ERP.Domain.Entities.StockTransferLine;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.StockTransfer;

public class StockTransferConfiguration : IEntityTypeConfiguration<StockTransferEntity>
{
    public void Configure(EntityTypeBuilder<StockTransferEntity> builder)
    {
        builder.ToTable("StockTransfers");

        builder.Property(s => s.Code).IsRequired().HasMaxLength(50);
        builder.Property(s => s.PostedBy).HasMaxLength(100);
        builder.Property(s => s.Notes).HasMaxLength(2000);

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(s => s.Code).IsUnique();
        builder.HasIndex(s => s.SourceWarehouseId);
        builder.HasIndex(s => s.DestinationWarehouseId);
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.TransferDate);

        builder.HasOne(s => s.SourceWarehouse)
            .WithMany()
            .HasForeignKey(s => s.SourceWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.DestinationWarehouse)
            .WithMany()
            .HasForeignKey(s => s.DestinationWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Lines)
            .WithOne(l => l.StockTransfer)
            .HasForeignKey(l => l.StockTransferId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(s => s.RowVersion).IsRowVersion();
    }
}

public class StockTransferLineConfiguration : IEntityTypeConfiguration<StockTransferLineEntity>
{
    public void Configure(EntityTypeBuilder<StockTransferLineEntity> builder)
    {
        builder.ToTable("StockTransferLines", t => t.HasCheckConstraint(
            "CK_StockTransferLine_OneItemType",
            "([RawMaterialId] IS NOT NULL AND [ProductId] IS NULL) OR ([RawMaterialId] IS NULL AND [ProductId] IS NOT NULL)"));

        builder.Property(l => l.Quantity).HasPrecision(18, 4);
        builder.Property(l => l.LineNotes).HasMaxLength(1000);

        builder.HasIndex(l => l.StockTransferId);
        builder.HasIndex(l => l.RawMaterialId);
        builder.HasIndex(l => l.ProductId);

        builder.HasOne(l => l.RawMaterial)
            .WithMany()
            .HasForeignKey(l => l.RawMaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Product)
            .WithMany()
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(l => l.RowVersion).IsRowVersion();
    }
}
