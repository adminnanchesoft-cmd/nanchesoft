using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class PayrollDispersionLineConfiguration : IEntityTypeConfiguration<PayrollDispersionLine>
{
    public void Configure(EntityTypeBuilder<PayrollDispersionLine> builder)
    {
        builder.ToTable("payroll_dispersion_lines");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EmployeeNumber).HasMaxLength(40).IsRequired();
        builder.Property(x => x.BeneficiaryName).HasMaxLength(180).IsRequired();
        builder.Property(x => x.BankName).HasMaxLength(120);
        builder.Property(x => x.BankAccount).HasMaxLength(60);
        builder.Property(x => x.Clabe).HasMaxLength(40);
        builder.Property(x => x.NetAmount).HasPrecision(18, 2);
        builder.Property(x => x.PaymentReference).HasMaxLength(80);
        builder.Property(x => x.ValidationStatus).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(1200);

        builder.HasIndex(x => new { x.PayrollDispersionBatchId, x.Sequence }).IsUnique();
        builder.HasIndex(x => x.PayrollRunLineId).IsUnique();

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PayrollDispersionBatch).WithMany().HasForeignKey(x => x.PayrollDispersionBatchId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.PayrollRun).WithMany().HasForeignKey(x => x.PayrollRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PayrollRunLine).WithMany().HasForeignKey(x => x.PayrollRunLineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}
