using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class ProductionScheduleLineConfiguration : IEntityTypeConfiguration<ProductionScheduleLine>
{
    public void Configure(EntityTypeBuilder<ProductionScheduleLine> builder)
    {
        builder.ToTable("production_schedule_lines");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Shift).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(1200);

        builder.HasIndex(x => x.ProductionScheduleId);
        builder.HasIndex(x => x.ProductionOrderId);
        builder.HasIndex(x => new { x.ProductionPhaseId, x.ScheduledDate });

        builder.HasOne(x => x.ProductionSchedule).WithMany(x => x.Lines).HasForeignKey(x => x.ProductionScheduleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.ProductionOrder).WithMany().HasForeignKey(x => x.ProductionOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductionOrderLine).WithMany().HasForeignKey(x => x.ProductionOrderLineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductionCell).WithMany().HasForeignKey(x => x.ProductionCellId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductionPhase).WithMany().HasForeignKey(x => x.ProductionPhaseId).OnDelete(DeleteBehavior.Restrict);
    }
}
