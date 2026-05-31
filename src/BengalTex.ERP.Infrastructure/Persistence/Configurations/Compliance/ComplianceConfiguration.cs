using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Compliance;

public class ComplianceCertificateConfiguration : IEntityTypeConfiguration<ComplianceCertificate>
{
    public void Configure(EntityTypeBuilder<ComplianceCertificate> builder)
    {
        builder.ToTable("ComplianceCertificates");

        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.IssuingAuthority).HasMaxLength(200);
        builder.Property(c => c.CertificateNumber).HasMaxLength(100);
        builder.Property(c => c.Notes).HasMaxLength(2000);

        builder.Property(c => c.CertificateType).HasConversion<string>().HasMaxLength(40);

        builder.HasIndex(c => c.CertificateType);
        builder.HasIndex(c => c.ExpiryDate);
        builder.HasIndex(c => c.IsActive);

        builder.Property(c => c.RowVersion).IsRowVersion();
    }
}

public class ComplianceAuditConfiguration : IEntityTypeConfiguration<ComplianceAudit>
{
    public void Configure(EntityTypeBuilder<ComplianceAudit> builder)
    {
        builder.ToTable("ComplianceAudits");

        builder.Property(a => a.Code).IsRequired().HasMaxLength(50);
        builder.Property(a => a.Auditor).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Notes).HasMaxLength(2000);

        builder.Property(a => a.AuditType).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.Result).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.Score).HasPrecision(5, 2);

        builder.HasIndex(a => a.Code).IsUnique();
        builder.HasIndex(a => a.AuditType);
        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => a.ScheduledDate);

        builder.HasMany(a => a.Findings)
            .WithOne(f => f.ComplianceAudit)
            .HasForeignKey(f => f.ComplianceAuditId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(a => a.RowVersion).IsRowVersion();
    }
}

public class AuditFindingConfiguration : IEntityTypeConfiguration<AuditFinding>
{
    public void Configure(EntityTypeBuilder<AuditFinding> builder)
    {
        builder.ToTable("AuditFindings");

        builder.Property(f => f.FindingDescription).IsRequired().HasMaxLength(2000);
        builder.Property(f => f.CorrectiveAction).HasMaxLength(2000);
        builder.Property(f => f.Notes).HasMaxLength(1000);

        builder.Property(f => f.Severity).HasConversion<string>().HasMaxLength(20);
        builder.Property(f => f.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(f => f.ComplianceAuditId);
        builder.HasIndex(f => f.Status);
        builder.HasIndex(f => f.Severity);
        builder.HasIndex(f => f.AssignedToEmployeeId);
        builder.HasIndex(f => f.DueDate);

        builder.HasOne(f => f.AssignedToEmployee)
            .WithMany()
            .HasForeignKey(f => f.AssignedToEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(f => f.RowVersion).IsRowVersion();
    }
}
