using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations.Production;

public sealed class ProductionPhaseAutoLinkConfiguration : IEntityTypeConfiguration<ProductionPhaseAutoLink>
{
    public void Configure(EntityTypeBuilder<ProductionPhaseAutoLink> builder)
    {
        builder.ToTable("production_phase_auto_links", "product", t =>
        {
            t.HasCheckConstraint("chk_phase_auto_links_no_self_loop",
                "\"from_phase_id\" <> \"to_phase_id\"");
        });
        builder.HasKey(x => x.Id);

        // UNIQUE 1:1: una fracción solo puede tener UN destino de auto-replicación
        builder.HasIndex(x => new { x.CompanyId, x.FromPhaseId }).IsUnique();
        builder.HasIndex(x => x.ToPhaseId);
        builder.HasIndex(x => x.SilvasoftChainId);

        builder.HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Company)
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.FromPhase)
            .WithMany()
            .HasForeignKey(x => x.FromPhaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ToPhase)
            .WithMany()
            .HasForeignKey(x => x.ToPhaseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
