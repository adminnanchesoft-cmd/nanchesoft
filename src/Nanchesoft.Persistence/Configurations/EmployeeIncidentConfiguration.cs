using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class EmployeeIncidentConfiguration : IEntityTypeConfiguration<EmployeeIncident>
{
    public void Configure(EntityTypeBuilder<EmployeeIncident> builder)
    {
        builder.ToTable("hr_employee_incidents");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PayrollIncidentTypeId).HasColumnName("payroll_incident_type_id").IsRequired();
        builder.Property(x => x.IncidentType).HasMaxLength(50);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Quantity).HasPrecision(18, 4);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.DeletedBy).HasMaxLength(120);

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PayrollPeriod).WithMany().HasForeignKey(x => x.PayrollPeriodId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PayrollIncidentType).WithMany().HasForeignKey(x => x.PayrollIncidentTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.RecurrentRule).WithMany().HasForeignKey(x => x.RecurrentRuleId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.CompanyId, x.PayrollIncidentTypeId, x.IncidentDate });
        builder.HasIndex(x => new { x.RecurrentRuleId, x.PayrollPeriodId }).IsUnique().HasFilter("recurrent_rule_id IS NOT NULL AND payroll_period_id IS NOT NULL");
        builder.HasIndex(x => new { x.TenantId, x.CompanyId, x.BranchId, x.IsActive, x.IsDeleted });
    }
}
