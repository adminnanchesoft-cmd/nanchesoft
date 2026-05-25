using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class MaterialInventoryMovementConfiguration : IEntityTypeConfiguration<MaterialInventoryMovement>
{
    public void Configure(EntityTypeBuilder<MaterialInventoryMovement> builder)
    {
        builder.ToTable("material_inventory_movements", "inventory");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.MaterialItemId, x.MovementDate });

        builder.Property(x => x.MovementType).HasMaxLength(40);
        builder.Property(x => x.DocumentType).HasMaxLength(60);
        builder.Property(x => x.DocumentFolio).HasMaxLength(40);
        builder.Property(x => x.UserName).HasMaxLength(120);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.QuantityIn).HasPrecision(18, 4);
        builder.Property(x => x.QuantityOut).HasPrecision(18, 4);
        builder.Property(x => x.BalanceAfter).HasPrecision(18, 4);
        builder.Property(x => x.UnitCost).HasPrecision(18, 4);
        builder.Property(x => x.TotalCost).HasPrecision(18, 2);

        builder.HasOne(x => x.MaterialItem).WithMany()
            .HasForeignKey(x => x.MaterialItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Warehouse).WithMany()
            .HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Supplier).WithMany()
            .HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
    }
}
