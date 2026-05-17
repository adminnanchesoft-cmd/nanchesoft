using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class MaterialRequirementLine : BaseEntity
{
    public Guid MaterialRequirementId { get; set; }
    public MaterialRequirement? MaterialRequirement { get; set; }

    public Guid? ProductionOrderLineId { get; set; }
    public ProductionOrderLine? ProductionOrderLine { get; set; }

    public Guid? ProductComponentId { get; set; }
    public ProductComponent? ProductComponent { get; set; }

    public Guid? MaterialItemId { get; set; }
    public MaterialItem? MaterialItem { get; set; }

    public Guid? UnitId { get; set; }
    public Unit? Unit { get; set; }

    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public string ComponentCode { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;

    public decimal QuantityRequired { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityReserved { get; set; }
    public decimal QuantityToReserve { get; set; }
    public decimal QuantityShortage { get; set; }
    public decimal QuantityOnOrder { get; set; }

    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }

    public string CoverageStatus { get; set; } = "unknown";
    // covered | partial | shortage | on_order | unknown

    public DateTime? ReservedAt { get; set; }
    public string? ReservedBy { get; set; }

    public Guid? PurchaseRequisitionId { get; set; }
    public PurchaseRequisition? PurchaseRequisition { get; set; }

    public Guid? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
}
