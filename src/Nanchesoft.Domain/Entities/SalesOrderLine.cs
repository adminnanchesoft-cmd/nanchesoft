using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class SalesOrderLine : BaseEntity
{
    public Guid SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }

    public int LineNumber { get; set; }

    public Guid? ItemId { get; set; }
    public Item? Item { get; set; }

    public Guid? UnitId { get; set; }
    public Unit? Unit { get; set; }

    public Guid? TaxId { get; set; }
    public Tax? Tax { get; set; }

    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal ShippedQuantity { get; set; }
    public decimal InvoicedQuantity { get; set; }
    public decimal PendingQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
}
