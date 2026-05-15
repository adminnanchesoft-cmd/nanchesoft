using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class ItemLotConfiguration : IEntityTypeConfiguration<ItemLot>
{
    public void Configure(EntityTypeBuilder<ItemLot> builder)
    {
        builder.ToTable("item_lots");
        builder.HasKey(x => x.Id);
    }
}
