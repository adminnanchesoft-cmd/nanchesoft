using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;
namespace Nanchesoft.Persistence.Configurations;
public sealed class InventoryEntryLineConfiguration : IEntityTypeConfiguration<InventoryEntryLine>
{
    public void Configure(EntityTypeBuilder<InventoryEntryLine> builder)
    {
        builder.ToTable("inventory_entry_lines");
        builder.HasKey(x => x.Id);
    }
}
