using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class ProductionInProcessConfiguration : IEntityTypeConfiguration<ProductionInProcess>
{
    public void Configure(EntityTypeBuilder<ProductionInProcess> builder)
    {
        builder.ToTable("production_in_process");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EnteredBy).HasMaxLength(120);
        builder.Property(x => x.Notes).HasMaxLength(1200);

        builder.HasIndex(x => new { x.ProductionOrderId, x.ProductionPhaseId });
        builder.HasIndex(x => new { x.ProductionPhaseId, x.EntryDate });

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductionOrder).WithMany().HasForeignKey(x => x.ProductionOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductionOrderLine).WithMany().HasForeignKey(x => x.ProductionOrderLineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductionPhase).WithMany().HasForeignKey(x => x.ProductionPhaseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductionCell).WithMany().HasForeignKey(x => x.ProductionCellId).OnDelete(DeleteBehavior.Restrict);
    }
}
