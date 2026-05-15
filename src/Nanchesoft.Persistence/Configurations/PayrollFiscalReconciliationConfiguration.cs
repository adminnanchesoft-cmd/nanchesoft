using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class PayrollFiscalReconciliationConfiguration : IEntityTypeConfiguration<PayrollFiscalReconciliation>
{
    public void Configure(EntityTypeBuilder<PayrollFiscalReconciliation> builder)
    {
        builder.ToTable("payroll_fiscal_reconciliations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReconciliationCode).HasMaxLength(40).IsRequired();
        builder.Property(x => x.GrossAmount).HasPrecision(18, 2);
        builder.Property(x => x.WithheldIsrAmount).HasPrecision(18, 2);
        builder.Property(x => x.EmployerTaxesAmount).HasPrecision(18, 2);
        builder.Property(x => x.NetAmount).HasPrecision(18, 2);
        builder.Property(x => x.DifferenceAmount).HasPrecision(18, 2);
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.ClosedBy).HasMaxLength(120);
        builder.Property(x => x.Notes).HasMaxLength(1200);

        builder.HasIndex(x => new { x.PayrollRunId, x.ReconciliationCode }).IsUnique();

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PayrollRun).WithMany().HasForeignKey(x => x.PayrollRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PayrollPeriod).WithMany().HasForeignKey(x => x.PayrollPeriodId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PayrollDispersionBatch).WithMany().HasForeignKey(x => x.PayrollDispersionBatchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PayrollAccountingPosting).WithMany().HasForeignKey(x => x.PayrollAccountingPostingId).OnDelete(DeleteBehavior.Restrict);
    }
}
