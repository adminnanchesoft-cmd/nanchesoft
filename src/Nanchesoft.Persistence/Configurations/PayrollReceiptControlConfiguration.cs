using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class PayrollReceiptControlConfiguration : IEntityTypeConfiguration<PayrollReceiptControl>
{
    public void Configure(EntityTypeBuilder<PayrollReceiptControl> builder)
    {
        builder.ToTable("payroll_receipt_controls");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReceiptNumber).HasMaxLength(40).IsRequired();
        builder.Property(x => x.ReceiptStatus).HasMaxLength(30).IsRequired();
        builder.Property(x => x.DeliveryChannel).HasMaxLength(40);
        builder.Property(x => x.DeliveryReference).HasMaxLength(120);
        builder.Property(x => x.AckBy).HasMaxLength(120);
        builder.Property(x => x.NetAmount).HasPrecision(18, 2);
        builder.Property(x => x.Notes).HasMaxLength(1200);

        builder.HasIndex(x => x.PayrollRunLineId).IsUnique();
        builder.HasIndex(x => new { x.PayrollRunId, x.ReceiptNumber }).IsUnique();

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PayrollRun).WithMany().HasForeignKey(x => x.PayrollRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PayrollRunLine).WithMany().HasForeignKey(x => x.PayrollRunLineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}
