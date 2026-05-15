using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class OnboardingChecklistRecordConfiguration : IEntityTypeConfiguration<OnboardingChecklistRecord>
{
    public void Configure(EntityTypeBuilder<OnboardingChecklistRecord> builder)
    {
        builder.ToTable("hr_onboarding_checklists");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ChecklistCode).HasMaxLength(40).IsRequired();
        builder.Property(x => x.ChecklistName).HasMaxLength(180).IsRequired();
        builder.Property(x => x.ResponsibleArea).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.CompletionPercent).HasPrecision(18, 2);
        builder.Property(x => x.Notes).HasMaxLength(800);

        builder.HasIndex(x => new { x.CompanyId, x.EmployeeId, x.ChecklistCode }).IsUnique();

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.CandidateApplication).WithMany().HasForeignKey(x => x.CandidateApplicationId).OnDelete(DeleteBehavior.Restrict);
    }
}
