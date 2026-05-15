using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class SuccessionPlanRecordConfiguration : IEntityTypeConfiguration<SuccessionPlanRecord>
{
    public void Configure(EntityTypeBuilder<SuccessionPlanRecord> builder)
    {
        builder.ToTable("hr_succession_plan_records");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PlanCode).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Criticality).HasMaxLength(30).IsRequired();
        builder.Property(x => x.ReadinessLevel).HasMaxLength(30).IsRequired();
        builder.Property(x => x.RiskOfLoss).HasMaxLength(30).IsRequired();
        builder.Property(x => x.DevelopmentPlan).HasMaxLength(400);
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(1200);

        builder.HasIndex(x => new { x.CompanyId, x.PlanCode }).IsUnique();

        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Position).WithMany().HasForeignKey(x => x.PositionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.IncumbentEmployee).WithMany().HasForeignKey(x => x.IncumbentEmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SuccessorEmployee).WithMany().HasForeignKey(x => x.SuccessorEmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}
