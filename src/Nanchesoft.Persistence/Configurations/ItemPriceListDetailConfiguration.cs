using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nanchesoft.Domain.Entities;

namespace Nanchesoft.Persistence.Configurations;

public sealed class ItemPriceListDetailConfiguration : IEntityTypeConfiguration<ItemPriceListDetail>
{
    public void Configure(EntityTypeBuilder<ItemPriceListDetail> builder)
    {
        builder.ToTable("item_price_list_details");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Price).HasPrecision(18, 2);

        builder.HasIndex(x => new { x.PriceListId, x.ItemId }).IsUnique();

        builder.HasOne(x => x.PriceList)
            .WithMany(x => x.Details)
            .HasForeignKey(x => x.PriceListId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Item)
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
