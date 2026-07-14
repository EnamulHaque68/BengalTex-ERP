using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Banking;

public class LcFinancialEventConfiguration : IEntityTypeConfiguration<LcFinancialEvent>
{
    public void Configure(EntityTypeBuilder<LcFinancialEvent> builder)
    {
        builder.ToTable("LcFinancialEvents");

        builder.Property(e => e.EventType).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.PaymentMethod).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.MarginApplied).HasPrecision(18, 2);
        builder.Property(e => e.Reference).HasMaxLength(100);
        builder.Property(e => e.Notes).HasMaxLength(1000);

        builder.HasIndex(e => e.LetterOfCreditId);
        builder.HasIndex(e => new { e.EventType, e.EventDate });

        builder.HasOne(e => e.LetterOfCredit)
            .WithMany(l => l.Events)
            .HasForeignKey(e => e.LetterOfCreditId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(e => e.RowVersion).IsRowVersion();
    }
}
