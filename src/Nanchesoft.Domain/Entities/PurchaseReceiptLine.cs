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

    public Guid? MaterialItemId { get; set; }
    public MaterialItem? MaterialItem { get; set; }

    public Guid? UnitId { get; set; }
    public Unit? Unit { get; set; }

    public Guid? TaxId { get; set; }
    public Tax? Tax { get; set; }

    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }

    // For comparison vs PO
    public decimal OrderedQuantity { get; set; }
    public decimal OrderedUnitPrice { get; set; }

    public string Notes { get; set; } = string.Empty;
}
