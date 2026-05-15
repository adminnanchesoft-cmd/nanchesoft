using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class PayrollSourceApplicationConfiguration : IEntityTypeConfiguration<PayrollSourceApplication>
{
    public void Configure(EntityTypeBuilder<PayrollSourceApplication> builder)
    {
        builder.ToTable("payroll_source_applications");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceType).HasMaxLength(40).IsRequired();
        builder.Property(x => x.ApplicationCode).HasMaxLength(40).IsRequired();
        builder.Property(x => x.ApplicationName).HasMaxLength(180).IsRequired();
        builder.Property(x => x.MovementType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Quantity).HasPrecision(18, 4);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.TaxableAmount).HasPrecision(18, 2);
        builder.Property(x => x.ExemptAmount).HasPrecision(18, 2);
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(1200);

        builder.HasIndex(x => new { x.PayrollRunId, x.EmployeeId, x.ApplicationCode, x.SourceId }).IsUnique();

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PayrollRun).WithMany().HasForeignKey(x => x.PayrollRunId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PayrollRunLine).WithMany().HasForeignKey(x => x.PayrollRunLineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PayrollPeriod).WithMany().HasForeignKey(x => x.PayrollPeriodId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PayrollConcept).WithMany().HasForeignKey(x => x.PayrollConceptId).OnDelete(DeleteBehavior.Restrict);
    }
}
