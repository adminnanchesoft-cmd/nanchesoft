using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class ItemBarcode : BaseEntity
{
    public Guid ItemId { get; set; }
    public Item? Item { get; set; }

    public string Barcode { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}
