using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class EmployeeCertificationRecordConfiguration : IEntityTypeConfiguration<EmployeeCertificationRecord>
{
    public void Configure(EntityTypeBuilder<EmployeeCertificationRecord> builder)
    {
        builder.ToTable("hr_employee_certifications");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CertificationCode).HasMaxLength(40).IsRequired();
        builder.Property(x => x.CertificationName).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(60).IsRequired();
        builder.Property(x => x.IssuedBy).HasMaxLength(160);
        builder.Property(x => x.Score).HasPrecision(18,2);
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(800);

        builder.HasIndex(x => new { x.CompanyId, x.EmployeeId, x.CertificationCode }).IsUnique();

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}
