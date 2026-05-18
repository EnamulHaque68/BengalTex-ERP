using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DeliveryNoteEntity = BengalTex.ERP.Domain.Entities.DeliveryNote;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.DeliveryNote;

public class DeliveryNoteConfiguration : IEntityTypeConfiguration<DeliveryNoteEntity>
{
    public void Configure(EntityTypeBuilder<DeliveryNoteEntity> builder)
    {
        builder.ToTable("DeliveryNotes");

        builder.Property(d => d.Code).IsRequired().HasMaxLength(50);
        builder.Property(d => d.VehicleNumber).HasMaxLength(50);
        builder.Property(d => d.DriverContact).HasMaxLength(100);
        builder.Property(d => d.DeliveryAddress).HasMaxLength(500);
        builder.Property(d => d.PostedBy).HasMaxLength(100);
        builder.Property(d => d.Notes).HasMaxLength(2000);

        builder.Property(d => d.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(d => d.Code).IsUnique();
        builder.HasIndex(d => d.SalesOrderId);
        builder.HasIndex(d => d.Status);
        builder.HasIndex(d => d.DispatchDate);

        // SO FK — Restrict so an SO with DNs can't be deleted out from under them
        builder.HasOne(d => d.SalesOrder)
            .WithMany()
            .HasForeignKey(d => d.SalesOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        // Dispatch warehouse FK — Restrict (required field)
        builder.HasOne(d => d.DispatchWarehouse)
            .WithMany()
            .HasForeignKey(d => d.DispatchWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        // One-to-many: DN → Lines
        builder.HasMany(d => d.Lines)
            .WithOne(l => l.DeliveryNote)
            .HasForeignKey(l => l.DeliveryNoteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(d => d.RowVersion).IsRowVersion();
    }
}

public class DeliveryNoteLineConfiguration : IEntityTypeConfiguration<DeliveryNoteLine>
{
    public void Configure(EntityTypeBuilder<DeliveryNoteLine> builder)
    {
        builder.ToTable("DeliveryNoteLines");

        builder.Property(l => l.DispatchedQuantity).HasPrecision(18, 4);
        builder.Property(l => l.LineNotes).HasMaxLength(1000);

        builder.HasIndex(l => l.DeliveryNoteId);
        builder.HasIndex(l => l.SalesOrderLineId);

        // SO line FK — Restrict so the originating SO line can be traced back
        builder.HasOne(l => l.SalesOrderLine)
            .WithMany()
            .HasForeignKey(l => l.SalesOrderLineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(l => l.RowVersion).IsRowVersion();
    }
}
