using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class EmployeeCompetencyAssessmentConfiguration : IEntityTypeConfiguration<EmployeeCompetencyAssessment>
{
    public void Configure(EntityTypeBuilder<EmployeeCompetencyAssessment> builder)
    {
        builder.ToTable("hr_employee_competency_assessments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AssessmentCode).HasMaxLength(40).IsRequired();
        builder.Property(x => x.CompetencyCode).HasMaxLength(40).IsRequired();
        builder.Property(x => x.CompetencyName).HasMaxLength(160).IsRequired();
        builder.Property(x => x.AssessorName).HasMaxLength(180).IsRequired();
        builder.Property(x => x.DevelopmentAction).HasMaxLength(400);
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(1200);

        builder.HasIndex(x => new { x.CompanyId, x.EmployeeId, x.AssessmentCode, x.CompetencyCode }).IsUnique();

        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Position).WithMany().HasForeignKey(x => x.PositionId).OnDelete(DeleteBehavior.Restrict);
    }
}
