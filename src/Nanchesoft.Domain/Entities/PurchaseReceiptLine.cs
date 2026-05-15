using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class PurchaseReceiptLine : BaseEntity
{
    public Guid PurchaseReceiptId { get; set; }
    public PurchaseReceipt? PurchaseReceipt { get; set; }

    public int LineNumber { get; set; }

    public Guid? PurchaseOrderLineId { get; set; }
    public PurchaseOrderLine? PurchaseOrderLine { get; set; }

    public Guid? ItemId { get; set; }
    public Item? Item { get; set; }

    public Guid? UnitId { get; set; }
    public Unit? Unit { get; set; }

    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
}
