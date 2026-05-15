using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class EmployeeDocumentRecordConfiguration : IEntityTypeConfiguration<EmployeeDocumentRecord>
{
    public void Configure(EntityTypeBuilder<EmployeeDocumentRecord> builder)
    {
        builder.ToTable("hr_employee_documents");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DocumentCode).HasMaxLength(30).IsRequired();
        builder.Property(x => x.DocumentName).HasMaxLength(160).IsRequired();
        builder.Property(x => x.DocumentType).HasMaxLength(60).IsRequired();
        builder.Property(x => x.DocumentNumber).HasMaxLength(80);
        builder.Property(x => x.FileName).HasMaxLength(180);
        builder.Property(x => x.FilePath).HasMaxLength(300);
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.VerifiedBy).HasMaxLength(120);
        builder.Property(x => x.Notes).HasMaxLength(800);

        builder.HasIndex(x => new { x.CompanyId, x.EmployeeId, x.DocumentCode }).IsUnique();

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}
