using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class PayrollDispersionBatchConfiguration : IEntityTypeConfiguration<PayrollDispersionBatch>
{
    public void Configure(EntityTypeBuilder<PayrollDispersionBatch> builder)
    {
        builder.ToTable("payroll_dispersion_batches");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BatchCode).HasMaxLength(40).IsRequired();
        builder.Property(x => x.LayoutFormat).HasMaxLength(30).IsRequired();
        builder.Property(x => x.BankName).HasMaxLength(120);
        builder.Property(x => x.FundingAccount).HasMaxLength(60);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.FileReference).HasMaxLength(240);
        builder.Property(x => x.Notes).HasMaxLength(1200);

        builder.HasIndex(x => new { x.PayrollRunId, x.BatchCode }).IsUnique();

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PayrollRun).WithMany().HasForeignKey(x => x.PayrollRunId).OnDelete(DeleteBehavior.Restrict);
    }
}
