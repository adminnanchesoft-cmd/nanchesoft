using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class ItemPriceListDetail : BaseEntity
{
    public Guid PriceListId { get; set; }
    public ItemPriceList? PriceList { get; set; }

    public Guid ItemId { get; set; }
    public Item? Item { get; set; }

    public decimal Price { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
}
