using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class MaterialRequirementLineConfiguration : IEntityTypeConfiguration<MaterialRequirementLine>
{
    public void Configure(EntityTypeBuilder<MaterialRequirementLine> builder)
    {
        builder.ToTable("material_requirement_lines");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ComponentCode).HasMaxLength(60);
        builder.Property(x => x.ComponentName).HasMaxLength(160);
        builder.Property(x => x.MaterialName).HasMaxLength(220);
        builder.Property(x => x.CoverageStatus).HasMaxLength(20).IsRequired();
        builder.Property(x => x.ReservedBy).HasMaxLength(120);

        builder.Property(x => x.QuantityRequired).HasPrecision(14, 4);
        builder.Property(x => x.QuantityOnHand).HasPrecision(14, 4);
        builder.Property(x => x.QuantityReserved).HasPrecision(14, 4);
        builder.Property(x => x.QuantityToReserve).HasPrecision(14, 4);
        builder.Property(x => x.QuantityShortage).HasPrecision(14, 4);
        builder.Property(x => x.QuantityOnOrder).HasPrecision(14, 4);
        builder.Property(x => x.UnitCost).HasPrecision(14, 4);
        builder.Property(x => x.TotalCost).HasPrecision(14, 4);

        builder.HasIndex(x => x.MaterialRequirementId);
        builder.HasIndex(x => x.MaterialItemId);
        builder.HasIndex(x => x.ProductComponentId);

        builder.HasOne(x => x.MaterialRequirement).WithMany(x => x.Lines).HasForeignKey(x => x.MaterialRequirementId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.ProductionOrderLine).WithMany().HasForeignKey(x => x.ProductionOrderLineId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProductComponent).WithMany().HasForeignKey(x => x.ProductComponentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.MaterialItem).WithMany().HasForeignKey(x => x.MaterialItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Unit).WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PurchaseRequisition).WithMany().HasForeignKey(x => x.PurchaseRequisitionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PurchaseOrder).WithMany().HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
    }
}
