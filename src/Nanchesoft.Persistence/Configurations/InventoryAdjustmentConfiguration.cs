using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;
namespace Nanchesoft.Persistence.Configurations;
public sealed class InventoryAdjustmentConfiguration : IEntityTypeConfiguration<InventoryAdjustment>
{
    public void Configure(EntityTypeBuilder<InventoryAdjustment> builder)
    {
        builder.ToTable("inventory_adjustments");
        builder.HasKey(x => x.Id);
        builder.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.InventoryAdjustmentId).OnDelete(DeleteBehavior.Cascade);
    }
}
