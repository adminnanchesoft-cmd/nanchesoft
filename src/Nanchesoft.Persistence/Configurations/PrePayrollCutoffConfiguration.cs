using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class PrePayrollCutoffConfiguration : IEntityTypeConfiguration<PrePayrollCutoff>
{
    public void Configure(EntityTypeBuilder<PrePayrollCutoff> builder)
    {
        builder.ToTable("payroll_prepayroll_cutoffs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CutoffCode).HasMaxLength(40).IsRequired();
        builder.Property(x => x.CutoffName).HasMaxLength(160).IsRequired();
        builder.Property(x => x.WorkedDaysTotal).HasPrecision(18, 4);
        builder.Property(x => x.OvertimeHoursTotal).HasPrecision(18, 4);
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(800);

        builder.HasIndex(x => new { x.CompanyId, x.PayrollPeriodId, x.CutoffCode }).IsUnique();

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PayrollPeriod).WithMany().HasForeignKey(x => x.PayrollPeriodId).OnDelete(DeleteBehavior.Restrict);
    }
}
