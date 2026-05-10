using BengalTex.ERP.Infrastructure.Persistence.CrossCutting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.CrossCutting;

public class NumberingSeriesConfiguration : IEntityTypeConfiguration<NumberingSeries>
{
    public void Configure(EntityTypeBuilder<NumberingSeries> builder)
    {
        builder.ToTable("NumberingSeries");

        builder.Property(n => n.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(n => n.Description)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(n => n.Prefix)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(n => n.Separator)
            .HasMaxLength(5)
            .IsRequired();

        // Enum as string
        builder.Property(n => n.ResetCycle)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Unique constraint: Code + FactoryId combination must be unique
        builder.HasIndex(n => new { n.Code, n.FactoryId })
            .IsUnique();
    }
}