using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesOrderEntity = BengalTex.ERP.Domain.Entities.SalesOrder;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.SalesOrder;

public class SalesOrderConfiguration : IEntityTypeConfiguration<SalesOrderEntity>
{
    public void Configure(EntityTypeBuilder<SalesOrderEntity> builder)
    {
        builder.ToTable("SalesOrders");

        builder.Property(s => s.Code).IsRequired().HasMaxLength(50);
        builder.Property(s => s.CustomerPoRef).HasMaxLength(100);
        builder.Property(s => s.DeliveryAddress).HasMaxLength(500);
        builder.Property(s => s.ConfirmedBy).HasMaxLength(100);
        builder.Property(s => s.Notes).HasMaxLength(2000);

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(s => s.Code).IsUnique();
        builder.HasIndex(s => s.CustomerId);
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.OrderDate);

        // Customer FK — Restrict so a customer with SOs can't be deleted
        builder.HasOne(s => s.Customer)
            .WithMany()
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // One-to-many: SO → Lines
        builder.HasMany(s => s.Lines)
            .WithOne(l => l.SalesOrder)
            .HasForeignKey(l => l.SalesOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(s => s.RowVersion).IsRowVersion();
    }
}

public class SalesOrderLineConfiguration : IEntityTypeConfiguration<SalesOrderLine>
{
    public void Configure(EntityTypeBuilder<SalesOrderLine> builder)
    {
        builder.ToTable("SalesOrderLines");

        builder.Property(l => l.Quantity).HasPrecision(18, 4);
        builder.Property(l => l.UnitPrice).HasPrecision(18, 4);
        builder.Property(l => l.LineNotes).HasMaxLength(1000);

        builder.HasIndex(l => l.SalesOrderId);
        builder.HasIndex(l => l.ProductId);

        // Product FK — Restrict so a product referenced by a SO line can't be deleted
        builder.HasOne(l => l.Product)
            .WithMany()
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(l => l.RowVersion).IsRowVersion();
    }
}
