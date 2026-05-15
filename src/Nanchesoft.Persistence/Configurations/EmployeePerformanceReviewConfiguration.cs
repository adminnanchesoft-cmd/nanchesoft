using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class EmployeePerformanceReviewConfiguration : IEntityTypeConfiguration<EmployeePerformanceReview>
{
    public void Configure(EntityTypeBuilder<EmployeePerformanceReview> builder)
    {
        builder.ToTable("hr_employee_performance_reviews");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReviewCode).HasMaxLength(40).IsRequired();
        builder.Property(x => x.ReviewCycle).HasMaxLength(60).IsRequired();
        builder.Property(x => x.ReviewerName).HasMaxLength(180).IsRequired();
        builder.Property(x => x.Score).HasPrecision(18, 2);
        builder.Property(x => x.CalibrationScore).HasPrecision(18, 2);
        builder.Property(x => x.GoalCompletionPercent).HasPrecision(18, 2);
        builder.Property(x => x.PotentialLevel).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(1200);

        builder.HasIndex(x => new { x.CompanyId, x.EmployeeId, x.ReviewCode }).IsUnique();

        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Position).WithMany().HasForeignKey(x => x.PositionId).OnDelete(DeleteBehavior.Restrict);
    }
}
