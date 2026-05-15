using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class PayrollEmployerObligationConfiguration : IEntityTypeConfiguration<PayrollEmployerObligation>
{
    public void Configure(EntityTypeBuilder<PayrollEmployerObligation> builder)
    {
        builder.ToTable("payroll_employer_obligations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ObligationCode).HasMaxLength(40).IsRequired();
        builder.Property(x => x.ObligationName).HasMaxLength(140).IsRequired();
        builder.Property(x => x.ObligationType).HasMaxLength(40).IsRequired();
        builder.Property(x => x.BaseAmount).HasPrecision(18, 2);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.ReferenceNumber).HasMaxLength(80);
        builder.Property(x => x.Notes).HasMaxLength(1200);

        builder.HasIndex(x => new { x.PayrollRunId, x.ObligationCode }).IsUnique();

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PayrollRun).WithMany().HasForeignKey(x => x.PayrollRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PayrollPeriod).WithMany().HasForeignKey(x => x.PayrollPeriodId).OnDelete(DeleteBehavior.Restrict);
    }
}
