using BengalTex.ERP.Infrastructure.Persistence.CrossCutting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.CrossCutting;

public class ApprovalRequestConfiguration : IEntityTypeConfiguration<ApprovalRequest>
{
    public void Configure(EntityTypeBuilder<ApprovalRequest> builder)
    {
        builder.ToTable("ApprovalRequests");

        builder.Property(a => a.DocumentType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.DocumentReference)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.RequestedBy).HasMaxLength(100);

        // Enum as string
        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Indexes
        builder.HasIndex(a => new { a.DocumentType, a.DocumentId });
        builder.HasIndex(a => a.Status);

        // One-to-many: ApprovalRequest → ApprovalSteps
        builder.HasMany(a => a.Steps)
            .WithOne(s => s.ApprovalRequest)
            .HasForeignKey(s => s.ApprovalRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ApprovalStepConfiguration : IEntityTypeConfiguration<ApprovalStep>
{
    public void Configure(EntityTypeBuilder<ApprovalStep> builder)
    {
        builder.ToTable("ApprovalSteps");

        builder.Property(s => s.ApproverRole)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.ApproverUserId).HasMaxLength(100);
        builder.Property(s => s.Comment).HasMaxLength(1000);

        // Enum as string
        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Indexes
        builder.HasIndex(s => s.ApprovalRequestId);
        builder.HasIndex(s => new { s.ApprovalRequestId, s.Level });
    }
}