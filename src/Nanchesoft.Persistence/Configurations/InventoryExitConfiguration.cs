using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;
namespace Nanchesoft.Persistence.Configurations;
public sealed class InventoryExitConfiguration : IEntityTypeConfiguration<InventoryExit>
{
    public void Configure(EntityTypeBuilder<InventoryExit> builder)
    {
        builder.ToTable("inventory_exits");
        builder.HasKey(x => x.Id);
        builder.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.InventoryExitId).OnDelete(DeleteBehavior.Cascade);
    }
}
