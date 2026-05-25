using System.ComponentModel.DataAnnotations.Schema;
using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class ProductionOrderLine : BaseEntity
{
    public Guid ProductionOrderId { get; set; }
    public ProductionOrder? ProductionOrder { get; set; }

    public int LineNumber { get; set; }

    public Guid? SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }

    public Guid? SalesOrderLineId { get; set; }
    public SalesOrderLine? SalesOrderLine { get; set; }

    public Guid FinishedProductId { get; set; }
    public FinishedProduct? FinishedProduct { get; set; }

    public Guid? ProductStyleId { get; set; }
    public ProductStyle? ProductStyle { get; set; }

    public Guid? ProductSizeRunId { get; set; }
    public ProductSizeRun? ProductSizeRun { get; set; }

    public Guid? ProductLastId { get; set; }
    public ProductLast? ProductLast { get; set; }

    public Guid? ProductColorId { get; set; }
    public ProductColor? ProductColor { get; set; }

    public Guid? ProductSoleId { get; set; }
    public ProductSole? ProductSole { get; set; }

    public Guid? ProductManufacturingTypeId { get; set; }
    public ProductManufacturingType? ProductManufacturingType { get; set; }

    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    // Quantities per size — stored as JSONB: { "sizeRunSizeId": quantity }
    [Column(TypeName = "jsonb")]
    public Dictionary<string, int> QuantitiesPerSize { get; set; } = new();

    public int TotalUnitsPlanned { get; set; }
    public int TotalUnitsProduced { get; set; }
    public int TotalUnitsShipped { get; set; }
    public int TotalUnitsPending { get; set; }

    public string Status { get; set; } = "pending";
    // pending | in_progress | completed | shipped | cancelled

    public DateOnly? DeliveryDate { get; set; }
    public int Priority { get; set; } = 1;
    public string Notes { get; set; } = string.Empty;

    // Número de control — unique per company, auto-generated
    public string NcFolio { get; set; } = string.Empty;

    // Commercial fields
    public string CustomerPoReference { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal LineTotal { get; set; }

    // Per-line shipping & identity overrides (defaults from order header)
    public Guid? WarehouseId { get; set; }
    public Guid? ShipToAddressId { get; set; }
    public string ShipToAddressText { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
}
