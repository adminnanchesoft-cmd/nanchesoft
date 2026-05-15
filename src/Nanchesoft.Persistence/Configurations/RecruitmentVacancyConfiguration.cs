using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class RecruitmentVacancyConfiguration : IEntityTypeConfiguration<RecruitmentVacancy>
{
    public void Configure(EntityTypeBuilder<RecruitmentVacancy> builder)
    {
        builder.ToTable("hr_recruitment_vacancies");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.VacancyCode).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(180).IsRequired();
        builder.Property(x => x.EmploymentType).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Headcount).IsRequired();
        builder.Property(x => x.SalaryMin).HasPrecision(18, 2);
        builder.Property(x => x.SalaryMax).HasPrecision(18, 2);
        builder.Property(x => x.HiringManager).HasMaxLength(120);
        builder.Property(x => x.Priority).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(800);

        builder.HasIndex(x => new { x.CompanyId, x.VacancyCode }).IsUnique();

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Position).WithMany().HasForeignKey(x => x.PositionId).OnDelete(DeleteBehavior.Restrict);
    }
}
