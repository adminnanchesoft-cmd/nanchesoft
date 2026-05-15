using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class InventoryEntryConfiguration : IEntityTypeConfiguration<InventoryEntry>
{
    public void Configure(EntityTypeBuilder<InventoryEntry> builder)
    {
        builder.ToTable("inventory_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Folio).HasMaxLength(32);
        builder.Property(x => x.Status).HasMaxLength(24);
        builder.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.InventoryEntryId).OnDelete(DeleteBehavior.Cascade);
    }
}
