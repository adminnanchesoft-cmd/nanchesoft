using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;
namespace Nanchesoft.Persistence.Configurations;
public sealed class InventoryTransferConfiguration : IEntityTypeConfiguration<InventoryTransfer>
{
    public void Configure(EntityTypeBuilder<InventoryTransfer> builder)
    {
        builder.ToTable("inventory_transfers");
        builder.HasKey(x => x.Id);
        builder.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.InventoryTransferId).OnDelete(DeleteBehavior.Cascade);
    }
}
