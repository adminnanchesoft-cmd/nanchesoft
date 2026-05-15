using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class PayrollTaxAccumulatorConfiguration : IEntityTypeConfiguration<PayrollTaxAccumulator>
{
    public void Configure(EntityTypeBuilder<PayrollTaxAccumulator> builder)
    {
        builder.ToTable("payroll_tax_accumulators");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AccumulatorCode).HasMaxLength(40).IsRequired();
        builder.Property(x => x.AccumulatorName).HasMaxLength(140).IsRequired();
        builder.Property(x => x.TaxableAmount).HasPrecision(18, 2);
        builder.Property(x => x.ExemptAmount).HasPrecision(18, 2);
        builder.Property(x => x.WithheldIsr).HasPrecision(18, 2);
        builder.Property(x => x.SubsidyApplied).HasPrecision(18, 2);
        builder.Property(x => x.SocialSecurityBase).HasPrecision(18, 2);
        builder.Property(x => x.NetAmount).HasPrecision(18, 2);
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(1200);

        builder.HasIndex(x => new { x.PayrollRunId, x.EmployeeId, x.AccumulatorCode }).IsUnique();

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PayrollRun).WithMany().HasForeignKey(x => x.PayrollRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PayrollRunLine).WithMany().HasForeignKey(x => x.PayrollRunLineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PayrollPeriod).WithMany().HasForeignKey(x => x.PayrollPeriodId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}
