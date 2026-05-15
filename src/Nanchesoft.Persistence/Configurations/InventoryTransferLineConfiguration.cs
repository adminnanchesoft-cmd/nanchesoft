using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;
namespace Nanchesoft.Persistence.Configurations;
public sealed class InventoryTransferLineConfiguration : IEntityTypeConfiguration<InventoryTransferLine>
{
    public void Configure(EntityTypeBuilder<InventoryTransferLine> builder)
    {
        builder.ToTable("inventory_transfer_lines");
        builder.HasKey(x => x.Id);
    }
}
