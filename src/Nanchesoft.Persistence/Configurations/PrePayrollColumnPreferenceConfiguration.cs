using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class PrePayrollColumnPreferenceConfiguration : IEntityTypeConfiguration<PrePayrollColumnPreference>
{
    public void Configure(EntityTypeBuilder<PrePayrollColumnPreference> builder)
    {
        builder.ToTable("payroll_prepayroll_column_preferences");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserKey).HasMaxLength(120);
        builder.Property(x => x.ConceptIds).HasMaxLength(4000);
        builder.Property(x => x.Notes).HasMaxLength(600);

        builder.HasIndex(x => new { x.CompanyId, x.UserKey, x.PayrollPeriodId }).IsUnique();

        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PayrollPeriod).WithMany().HasForeignKey(x => x.PayrollPeriodId).OnDelete(DeleteBehavior.Restrict);
    }
}
