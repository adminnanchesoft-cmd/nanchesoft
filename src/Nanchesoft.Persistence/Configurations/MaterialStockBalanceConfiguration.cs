using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class MaterialStockBalanceConfiguration : IEntityTypeConfiguration<MaterialStockBalance>
{
    public void Configure(EntityTypeBuilder<MaterialStockBalance> builder)
    {
        builder.ToTable("material_stock_balances", "inventory");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.CompanyId, x.WarehouseId, x.MaterialItemId }).IsUnique();

        builder.Property(x => x.QuantityOnHand).HasPrecision(18, 4);
        builder.Property(x => x.QuantityReserved).HasPrecision(18, 4);
        builder.Property(x => x.QuantityAvailable).HasPrecision(18, 4);
        builder.Property(x => x.AverageCost).HasPrecision(18, 4);
        builder.Property(x => x.LastCost).HasPrecision(18, 4);

        builder.HasOne(x => x.MaterialItem).WithMany()
            .HasForeignKey(x => x.MaterialItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Warehouse).WithMany()
            .HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
    }
}
