using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.BankReconciliation;

public class BankStatementConfiguration : IEntityTypeConfiguration<BankStatement>
{
    public void Configure(EntityTypeBuilder<BankStatement> builder)
    {
        builder.ToTable("BankStatements");

        builder.Property(s => s.Code).IsRequired().HasMaxLength(50);
        builder.Property(s => s.Notes).HasMaxLength(2000);
        builder.Property(s => s.ReconciledBy).HasMaxLength(100);

        builder.Property(s => s.OpeningBalance).HasPrecision(18, 2);
        builder.Property(s => s.ClosingBalance).HasPrecision(18, 2);

        builder.HasIndex(s => s.Code).IsUnique();
        builder.HasIndex(s => s.BankAccountId);
        builder.HasIndex(s => s.StatementDate);
        builder.HasIndex(s => s.IsReconciled);

        builder.HasOne(s => s.BankAccount)
            .WithMany()
            .HasForeignKey(s => s.BankAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.Lines)
            .WithOne(l => l.BankStatement)
            .HasForeignKey(l => l.BankStatementId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(s => s.RowVersion).IsRowVersion();
    }
}

public class BankStatementLineConfiguration : IEntityTypeConfiguration<BankStatementLine>
{
    public void Configure(EntityTypeBuilder<BankStatementLine> builder)
    {
        builder.ToTable("BankStatementLines");

        builder.Property(l => l.Description).IsRequired().HasMaxLength(500);
        builder.Property(l => l.ReferenceNumber).HasMaxLength(100);
        builder.Property(l => l.Notes).HasMaxLength(1000);
        builder.Property(l => l.MatchedBy).HasMaxLength(100);

        builder.Property(l => l.Amount).HasPrecision(18, 2);
        builder.Property(l => l.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(l => l.BankStatementId);
        builder.HasIndex(l => l.Status);
        builder.HasIndex(l => l.TransactionDate);
        builder.HasIndex(l => l.MatchedJournalLineId);

        builder.HasOne(l => l.MatchedJournalLine)
            .WithMany()
            .HasForeignKey(l => l.MatchedJournalLineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(l => l.RowVersion).IsRowVersion();
    }
}
