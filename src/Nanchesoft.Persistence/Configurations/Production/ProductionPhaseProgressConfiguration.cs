using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class ProductionPhaseProgressConfiguration : IEntityTypeConfiguration<ProductionPhaseProgress>
{
    public void Configure(EntityTypeBuilder<ProductionPhaseProgress> builder)
    {
        builder.ToTable("production_phase_progress");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.LastRescheduleReason).HasMaxLength(1000);

        builder.HasIndex(x => new { x.ProductionOrderId, x.ProductionOrderLineId, x.ProductionPhaseId }).IsUnique();
        builder.HasIndex(x => x.ProductionOrderId);

        builder.HasOne(x => x.ProductionOrder).WithMany(x => x.PhaseProgress).HasForeignKey(x => x.ProductionOrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.ProductionOrderLine).WithMany().HasForeignKey(x => x.ProductionOrderLineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductionPhase).WithMany().HasForeignKey(x => x.ProductionPhaseId).OnDelete(DeleteBehavior.Restrict);
    }
}
