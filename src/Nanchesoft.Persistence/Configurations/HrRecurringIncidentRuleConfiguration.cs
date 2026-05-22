using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class HrRecurringIncidentRuleConfiguration : IEntityTypeConfiguration<HrRecurringIncidentRule>
{
    public void Configure(EntityTypeBuilder<HrRecurringIncidentRule> builder)
    {
        builder.ToTable("hr_recurring_incident_rules");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Frequency).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.AuthorizedBy).HasMaxLength(120);
        builder.Property(x => x.DeletedBy).HasMaxLength(120);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Quantity).HasPrecision(18, 4);

        builder.HasIndex(x => new { x.TenantId, x.CompanyId, x.BranchId, x.IsActive, x.IsDeleted });
        builder.HasIndex(x => new { x.CompanyId, x.EmployeeId, x.NomPayrollIncidentTypeId, x.StartDate });

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.NomPayrollIncidentType).WithMany().HasForeignKey(x => x.NomPayrollIncidentTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}
