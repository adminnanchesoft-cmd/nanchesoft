using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class NomPayrollIncidentTypeConfiguration : IEntityTypeConfiguration<NomPayrollIncidentType>
{
    public void Configure(EntityTypeBuilder<NomPayrollIncidentType> builder)
    {
        builder.ToTable("nom_payroll_incident_types");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(180).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.IncidentCategory).HasMaxLength(30).IsRequired();
        builder.Property(x => x.AffectType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.PayrollConceptType).HasMaxLength(40).IsRequired();
        builder.Property(x => x.SatCode).HasMaxLength(30);
        builder.Property(x => x.Color).HasMaxLength(20);
        builder.Property(x => x.Icon).HasMaxLength(60);
        builder.Property(x => x.DeletedBy).HasMaxLength(120);

        builder.HasIndex(x => new { x.TenantId, x.CompanyId, x.Code }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.CompanyId, x.BranchId, x.IsActive, x.IsDeleted });
        builder.HasIndex(x => new { x.CompanyId, x.IncidentCategory, x.SortOrder });

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}
