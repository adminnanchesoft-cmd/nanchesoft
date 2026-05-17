using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class ProductionOrderConfiguration : IEntityTypeConfiguration<ProductionOrder>
{
    public void Configure(EntityTypeBuilder<ProductionOrder> builder)
    {
        builder.ToTable("production_orders");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Folio).HasMaxLength(20).IsRequired();
        builder.Property(x => x.WeekCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.ExplosionStatus).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.ApprovedBy).HasMaxLength(120);
        builder.Property(x => x.ClosedBy).HasMaxLength(120);

        builder.HasIndex(x => new { x.TenantId, x.CompanyId, x.Folio }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.CompanyId, x.WeekCode });
        builder.HasIndex(x => new { x.TenantId, x.CompanyId, x.Status });
        builder.HasIndex(x => new { x.TenantId, x.CompanyId, x.DeliveryDate });

        builder.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Lines).WithOne(x => x.ProductionOrder).HasForeignKey(x => x.ProductionOrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.MaterialRequirements).WithOne(x => x.ProductionOrder).HasForeignKey(x => x.ProductionOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.PhaseProgress).WithOne(x => x.ProductionOrder).HasForeignKey(x => x.ProductionOrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Vouchers).WithOne(x => x.ProductionOrder).HasForeignKey(x => x.ProductionOrderId).OnDelete(DeleteBehavior.Restrict);
    }
}
