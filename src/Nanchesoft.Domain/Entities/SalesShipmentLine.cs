using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class SalesShipmentLine : BaseEntity
{
    public Guid SalesShipmentId { get; set; }
    public SalesShipment? SalesShipment { get; set; }

    public int LineNumber { get; set; }

    public Guid? SalesOrderLineId { get; set; }
    public SalesOrderLine? SalesOrderLine { get; set; }

    public Guid? ItemId { get; set; }
    public Item? Item { get; set; }

    public Guid? UnitId { get; set; }
    public Unit? Unit { get; set; }

    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
}
