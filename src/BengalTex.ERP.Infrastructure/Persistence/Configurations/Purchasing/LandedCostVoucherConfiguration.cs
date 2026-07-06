using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Purchasing;

public class LandedCostVoucherConfiguration : IEntityTypeConfiguration<LandedCostVoucher>
{
    public void Configure(EntityTypeBuilder<LandedCostVoucher> builder)
    {
        builder.ToTable("LandedCostVouchers");

        builder.Property(v => v.Code).IsRequired().HasMaxLength(50);
        builder.Property(v => v.PostedBy).HasMaxLength(100);
        builder.Property(v => v.Notes).HasMaxLength(2000);
        builder.Property(v => v.AllocationBasis).HasConversion<string>().HasMaxLength(20);
        builder.Property(v => v.PaymentMethod).HasConversion<string>().HasMaxLength(20);
        builder.Property(v => v.Status).HasConversion<string>().HasMaxLength(20);
        // Phase A2 — on-credit settlement
        builder.Property(v => v.SettledBy).HasMaxLength(100);
        builder.Property(v => v.SettlementMethod).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(v => v.Code).IsUnique();
        builder.HasIndex(v => v.Status);
        builder.HasIndex(v => v.GoodsReceiptNoteId);
        builder.HasIndex(v => v.SupplierId);

        // GRN FK — Restrict so a costed receipt can't be deleted out from under the voucher
        builder.HasOne(v => v.GoodsReceiptNote)
            .WithMany()
            .HasForeignKey(v => v.GoodsReceiptNoteId)
            .OnDelete(DeleteBehavior.Restrict);

        // Phase A2 — optional C&F agent / supplier owed on-credit charges
        builder.HasOne(v => v.Supplier)
            .WithMany()
            .HasForeignKey(v => v.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(v => v.Charges)
            .WithOne(c => c.LandedCostVoucher)
            .HasForeignKey(c => c.LandedCostVoucherId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(v => v.RowVersion).IsRowVersion();
    }
}

public class LandedCostChargeConfiguration : IEntityTypeConfiguration<LandedCostCharge>
{
    public void Configure(EntityTypeBuilder<LandedCostCharge> builder)
    {
        builder.ToTable("LandedCostCharges");

        builder.Property(c => c.ChargeType).HasConversion<string>().HasMaxLength(30);
        builder.Property(c => c.Amount).HasPrecision(18, 2);
        builder.Property(c => c.Notes).HasMaxLength(500);

        builder.HasIndex(c => c.LandedCostVoucherId);

        builder.Property(c => c.RowVersion).IsRowVersion();
    }
}
