using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Accounting;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");

        builder.Property(a => a.Code).IsRequired().HasMaxLength(30);
        builder.Property(a => a.Name).IsRequired().HasMaxLength(150);
        builder.Property(a => a.Description).HasMaxLength(500);

        builder.Property(a => a.AccountType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(a => a.Code).IsUnique();
        builder.HasIndex(a => a.AccountType);
        builder.HasIndex(a => a.ParentAccountId);

        // Self-referencing hierarchy — Restrict so a parent with children can't be deleted
        builder.HasOne(a => a.ParentAccount)
            .WithMany(a => a.Children)
            .HasForeignKey(a => a.ParentAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(a => a.RowVersion).IsRowVersion();
    }
}

public class JournalEntryConfiguration : IEntityTypeConfiguration<JournalEntry>
{
    public void Configure(EntityTypeBuilder<JournalEntry> builder)
    {
        builder.ToTable("JournalEntries");

        builder.Property(j => j.Code).IsRequired().HasMaxLength(50);
        builder.Property(j => j.Reference).HasMaxLength(100);
        builder.Property(j => j.Narration).HasMaxLength(1000);
        builder.Property(j => j.SourceType).HasMaxLength(50);
        builder.Property(j => j.SourceCode).HasMaxLength(50);
        builder.Property(j => j.PostedBy).HasMaxLength(100);

        builder.Property(j => j.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(j => j.Code).IsUnique();
        builder.HasIndex(j => j.Status);
        builder.HasIndex(j => j.EntryDate);
        builder.HasIndex(j => new { j.SourceType, j.SourceId });

        builder.HasMany(j => j.Lines)
            .WithOne(l => l.JournalEntry)
            .HasForeignKey(l => l.JournalEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(j => j.RowVersion).IsRowVersion();
    }
}

public class JournalEntryLineConfiguration : IEntityTypeConfiguration<JournalEntryLine>
{
    public void Configure(EntityTypeBuilder<JournalEntryLine> builder)
    {
        builder.ToTable("JournalEntryLines");

        builder.Property(l => l.Debit).HasPrecision(18, 2);
        builder.Property(l => l.Credit).HasPrecision(18, 2);
        builder.Property(l => l.LineNarration).HasMaxLength(500);

        builder.HasIndex(l => l.JournalEntryId);
        builder.HasIndex(l => l.AccountId);

        // Account FK — Restrict so an account with postings can't be deleted
        builder.HasOne(l => l.Account)
            .WithMany()
            .HasForeignKey(l => l.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(l => l.RowVersion).IsRowVersion();
    }
}
