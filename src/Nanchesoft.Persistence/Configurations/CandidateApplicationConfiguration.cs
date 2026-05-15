using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class CandidateApplicationConfiguration : IEntityTypeConfiguration<CandidateApplication>
{
    public void Configure(EntityTypeBuilder<CandidateApplication> builder)
    {
        builder.ToTable("hr_candidate_applications");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CandidateCode).HasMaxLength(40).IsRequired();
        builder.Property(x => x.FullName).HasMaxLength(180).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(180);
        builder.Property(x => x.Phone).HasMaxLength(40);
        builder.Property(x => x.Source).HasMaxLength(60).IsRequired();
        builder.Property(x => x.Stage).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Score).HasPrecision(18, 2);
        builder.Property(x => x.OfferAmount).HasPrecision(18, 2);
        builder.Property(x => x.CvFileName).HasMaxLength(180);
        builder.Property(x => x.CvFilePath).HasMaxLength(300);
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(800);

        builder.HasIndex(x => new { x.CompanyId, x.CandidateCode }).IsUnique();

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.RecruitmentVacancy).WithMany().HasForeignKey(x => x.RecruitmentVacancyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.HiredEmployee).WithMany().HasForeignKey(x => x.HiredEmployeeId).OnDelete(DeleteBehavior.Restrict);
    }
}
