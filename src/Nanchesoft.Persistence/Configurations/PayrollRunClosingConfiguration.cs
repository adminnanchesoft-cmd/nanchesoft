using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class PayrollRunClosingConfiguration : IEntityTypeConfiguration<PayrollRunClosing>
{
    public void Configure(EntityTypeBuilder<PayrollRunClosing> builder)
    {
        builder.ToTable("payroll_run_closings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ClosingCode).HasMaxLength(40).IsRequired();
        builder.Property(x => x.GrossAmount).HasPrecision(18, 2);
        builder.Property(x => x.DeductionsAmount).HasPrecision(18, 2);
        builder.Property(x => x.NetAmount).HasPrecision(18, 2);
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.ClosedBy).HasMaxLength(120);
        builder.Property(x => x.Notes).HasMaxLength(1200);

        builder.HasIndex(x => new { x.PayrollRunId, x.ClosingCode }).IsUnique();

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PayrollRun).WithMany().HasForeignKey(x => x.PayrollRunId).OnDelete(DeleteBehavior.Restrict);
    }
}
