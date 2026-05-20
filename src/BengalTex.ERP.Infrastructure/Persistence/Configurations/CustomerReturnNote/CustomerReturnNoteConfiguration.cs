using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.CustomerReturnNote;

public class CustomerReturnNoteConfiguration : IEntityTypeConfiguration<Domain.Entities.CustomerReturnNote>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.CustomerReturnNote> builder)
    {
        builder.ToTable("CustomerReturnNotes");

        builder.Property(c => c.Code).IsRequired().HasMaxLength(50);
        builder.Property(c => c.VehicleNumber).HasMaxLength(50);
        builder.Property(c => c.Reason).HasMaxLength(500);
        builder.Property(c => c.PostedBy).HasMaxLength(100);
        builder.Property(c => c.Notes).HasMaxLength(2000);

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(c => c.Code).IsUnique();
        builder.HasIndex(c => c.DeliveryNoteId);
        builder.HasIndex(c => c.ReturnWarehouseId);
        builder.HasIndex(c => c.Status);
        builder.HasIndex(c => c.ReturnDate);

        builder.HasOne(c => c.DeliveryNote)
            .WithMany()
            .HasForeignKey(c => c.DeliveryNoteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.ReturnWarehouse)
            .WithMany()
            .HasForeignKey(c => c.ReturnWarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Lines)
            .WithOne(l => l.CustomerReturnNote)
            .HasForeignKey(l => l.CustomerReturnNoteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(c => c.RowVersion).IsRowVersion();
    }
}

public class CustomerReturnNoteLineConfiguration : IEntityTypeConfiguration<CustomerReturnNoteLine>
{
    public void Configure(EntityTypeBuilder<CustomerReturnNoteLine> builder)
    {
        builder.ToTable("CustomerReturnNoteLines");

        builder.Property(l => l.ReturnedQuantity).HasPrecision(18, 4);
        builder.Property(l => l.LineNotes).HasMaxLength(1000);

        builder.HasIndex(l => l.CustomerReturnNoteId);
        builder.HasIndex(l => l.DeliveryNoteLineId);
        builder.HasIndex(l => l.ProductId);

        builder.HasOne(l => l.DeliveryNoteLine)
            .WithMany()
            .HasForeignKey(l => l.DeliveryNoteLineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Product)
            .WithMany()
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(l => l.RowVersion).IsRowVersion();
    }
}
