using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;
namespace Nanchesoft.Persistence.Configurations;
public sealed class InventoryExitLineConfiguration : IEntityTypeConfiguration<InventoryExitLine>
{
    public void Configure(EntityTypeBuilder<InventoryExitLine> builder)
    {
        builder.ToTable("inventory_exit_lines");
        builder.HasKey(x => x.Id);
    }
}
