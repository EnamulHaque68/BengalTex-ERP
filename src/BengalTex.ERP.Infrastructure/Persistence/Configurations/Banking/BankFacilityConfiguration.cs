using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Banking;

public class BankFacilityConfiguration : IEntityTypeConfiguration<BankFacility>
{
    public void Configure(EntityTypeBuilder<BankFacility> builder)
    {
        builder.ToTable("BankFacilities");

        builder.Property(f => f.Code).IsRequired().HasMaxLength(50);
        builder.Property(f => f.FacilityType).HasConversion<string>().HasMaxLength(20);
        builder.Property(f => f.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(f => f.BankName).IsRequired().HasMaxLength(200);
        builder.Property(f => f.AccountReference).HasMaxLength(100);
        builder.Property(f => f.Amount).HasPrecision(18, 2);
        builder.Property(f => f.InterestRate).HasPrecision(9, 4);
        builder.Property(f => f.Notes).HasMaxLength(1000);

        builder.HasIndex(f => f.Code).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(f => f.Status);

        builder.Property(f => f.RowVersion).IsRowVersion();
    }
}

public class BankFacilityEventConfiguration : IEntityTypeConfiguration<BankFacilityEvent>
{
    public void Configure(EntityTypeBuilder<BankFacilityEvent> builder)
    {
        builder.ToTable("BankFacilityEvents");

        builder.Property(e => e.EventType).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.PaymentMethod).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.Reference).HasMaxLength(100);
        builder.Property(e => e.Notes).HasMaxLength(1000);

        builder.HasIndex(e => e.BankFacilityId);
        builder.HasIndex(e => new { e.EventType, e.EventDate });

        builder.HasOne(e => e.BankFacility)
            .WithMany(f => f.Events)
            .HasForeignKey(e => e.BankFacilityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(e => e.RowVersion).IsRowVersion();
    }
}
