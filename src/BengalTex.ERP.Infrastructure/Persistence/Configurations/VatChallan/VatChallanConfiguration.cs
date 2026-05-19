using BengalTex.ERP.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BengalTex.ERP.Infrastructure.Persistence.Configurations.VatChallan;

public class VatChallanConfiguration : IEntityTypeConfiguration<Domain.Entities.VatChallan>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.VatChallan> builder)
    {
        builder.ToTable("VatChallans");

        builder.Property(v => v.Code).IsRequired().HasMaxLength(50);
        builder.Property(v => v.Notes).HasMaxLength(2000);
        builder.Property(v => v.SubtotalAmount).HasPrecision(18, 4);
        builder.Property(v => v.VatAmount).HasPrecision(18, 4);
        builder.Property(v => v.TotalAmount).HasPrecision(18, 4);

        builder.HasIndex(v => v.Code).IsUnique();
        builder.HasIndex(v => v.ChallanDate);

        // 1-to-1 with CustomerInvoice — at most one challan per invoice.
        // Filtered unique index allows soft-deleted challans to coexist with new ones if
        // an invoice is cancelled + reissued (cancellation soft-deletes the original).
        builder.HasIndex(v => v.CustomerInvoiceId)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("UX_VatChallans_CustomerInvoice");

        builder.HasOne(v => v.CustomerInvoice)
            .WithOne(c => c.VatChallan)
            .HasForeignKey<Domain.Entities.VatChallan>(v => v.CustomerInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(v => v.RowVersion).IsRowVersion();
    }
}
