using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class PhaseRestrictionConfiguration : IEntityTypeConfiguration<PhaseRestriction>
{
    public void Configure(EntityTypeBuilder<PhaseRestriction> builder)
    {
        builder.ToTable("phase_restrictions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RestrictionType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(500);

        builder.HasIndex(x => new { x.CompanyId, x.ProductionPhaseId });

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.FinishedProduct).WithMany().HasForeignKey(x => x.FinishedProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductStyle).WithMany().HasForeignKey(x => x.ProductStyleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductionPhase).WithMany().HasForeignKey(x => x.ProductionPhaseId).OnDelete(DeleteBehavior.Restrict);
    }
}
