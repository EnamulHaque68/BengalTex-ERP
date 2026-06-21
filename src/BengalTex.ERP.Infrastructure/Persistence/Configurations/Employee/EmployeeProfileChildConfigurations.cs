using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.Employee;

public class EmployeeEducationConfiguration : IEntityTypeConfiguration<EmployeeEducation>
{
    public void Configure(EntityTypeBuilder<EmployeeEducation> builder)
    {
        builder.ToTable("EmployeeEducations");

        builder.Property(e => e.Degree).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Institute).HasMaxLength(200);
        builder.Property(e => e.Result).HasMaxLength(100);
        builder.HasIndex(e => e.EmployeeId);

        builder.HasOne(e => e.Employee)
            .WithMany()
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(e => e.RowVersion).IsRowVersion();
    }
}

public class EmployeeEmergencyContactConfiguration : IEntityTypeConfiguration<EmployeeEmergencyContact>
{
    public void Configure(EntityTypeBuilder<EmployeeEmergencyContact> builder)
    {
        builder.ToTable("EmployeeEmergencyContacts");

        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Relationship).HasMaxLength(50);
        builder.Property(e => e.Phone).IsRequired().HasMaxLength(30);
        builder.Property(e => e.Address).HasMaxLength(500);
        builder.HasIndex(e => e.EmployeeId);

        builder.HasOne(e => e.Employee)
            .WithMany()
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(e => e.RowVersion).IsRowVersion();
    }
}
