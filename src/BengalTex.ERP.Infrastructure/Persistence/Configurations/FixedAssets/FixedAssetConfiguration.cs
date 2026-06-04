using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.FixedAssets;

public class FixedAssetConfiguration : IEntityTypeConfiguration<FixedAsset>
{
    public void Configure(EntityTypeBuilder<FixedAsset> builder)
    {
        builder.ToTable("FixedAssets");

        builder.Property(a => a.Code).IsRequired().HasMaxLength(50);
        builder.Property(a => a.Name).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Location).HasMaxLength(150);
        builder.Property(a => a.DisposalNotes).HasMaxLength(1000);
        builder.Property(a => a.DisposedByUser).HasMaxLength(100);
        builder.Property(a => a.Notes).HasMaxLength(2000);

        builder.Property(a => a.AcquisitionCost).HasPrecision(18, 2);
        builder.Property(a => a.SalvageValue).HasPrecision(18, 2);
        builder.Property(a => a.AccumulatedDepreciation).HasPrecision(18, 2);
        builder.Property(a => a.DisposalProceeds).HasPrecision(18, 2);

        builder.Property(a => a.Category).HasConversion<string>().HasMaxLength(30);
        builder.Property(a => a.DepreciationMethod).HasConversion<string>().HasMaxLength(30);
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(a => a.Code).IsUnique();
        builder.HasIndex(a => a.Category);
        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => a.MachineId);

        builder.HasOne(a => a.Machine).WithMany()
            .HasForeignKey(a => a.MachineId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(a => a.RowVersion).IsRowVersion();
    }
}

public class AssetDepreciationRunConfiguration : IEntityTypeConfiguration<AssetDepreciationRun>
{
    public void Configure(EntityTypeBuilder<AssetDepreciationRun> builder)
    {
        builder.ToTable("AssetDepreciationRuns");

        builder.Property(r => r.Code).IsRequired().HasMaxLength(50);
        builder.Property(r => r.TotalAmount).HasPrecision(18, 2);
        builder.Property(r => r.PostedByUser).HasMaxLength(100);
        builder.Property(r => r.Notes).HasMaxLength(1000);

        builder.HasIndex(r => r.Code).IsUnique();
        builder.HasIndex(r => new { r.Year, r.Month }).IsUnique();    // one run per month

        builder.HasMany(r => r.Lines)
            .WithOne(l => l.AssetDepreciationRun)
            .HasForeignKey(l => l.AssetDepreciationRunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(r => r.RowVersion).IsRowVersion();
    }
}

public class AssetDepreciationRunLineConfiguration : IEntityTypeConfiguration<AssetDepreciationRunLine>
{
    public void Configure(EntityTypeBuilder<AssetDepreciationRunLine> builder)
    {
        builder.ToTable("AssetDepreciationRunLines");

        builder.Property(l => l.MonthlyDepreciation).HasPrecision(18, 2);
        builder.Property(l => l.AccumulatedAfter).HasPrecision(18, 2);
        builder.Property(l => l.NetBookValueAfter).HasPrecision(18, 2);

        builder.HasIndex(l => l.AssetDepreciationRunId);
        builder.HasIndex(l => l.FixedAssetId);

        builder.HasOne(l => l.FixedAsset).WithMany()
            .HasForeignKey(l => l.FixedAssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(l => l.RowVersion).IsRowVersion();
    }
}
