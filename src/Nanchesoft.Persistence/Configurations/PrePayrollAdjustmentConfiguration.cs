using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class PrePayrollAdjustmentConfiguration : IEntityTypeConfiguration<PrePayrollAdjustment>
{
    public void Configure(EntityTypeBuilder<PrePayrollAdjustment> builder)
    {
        builder.ToTable("payroll_prepayroll_adjustments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AdjustmentCode).HasMaxLength(40).IsRequired();
        builder.Property(x => x.AdjustmentName).HasMaxLength(160).IsRequired();
        builder.Property(x => x.AdjustmentType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.CaptureSource).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Quantity).HasPrecision(18, 4);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.TaxableAmount).HasPrecision(18, 2);
        builder.Property(x => x.ExemptAmount).HasPrecision(18, 2);
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(800);

        builder.HasIndex(x => new { x.CompanyId, x.EmployeeId, x.PayrollPeriodId, x.AdjustmentCode }).IsUnique();

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PayrollPeriod).WithMany().HasForeignKey(x => x.PayrollPeriodId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PayrollConcept).WithMany().HasForeignKey(x => x.PayrollConceptId).OnDelete(DeleteBehavior.Restrict);
    }
}
