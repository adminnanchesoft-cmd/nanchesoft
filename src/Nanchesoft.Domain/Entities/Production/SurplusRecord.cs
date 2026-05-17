using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class SurplusRecord : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid ProductionOrderId { get; set; }
    public ProductionOrder? ProductionOrder { get; set; }

    public Guid FinishedProductId { get; set; }
    public FinishedProduct? FinishedProduct { get; set; }

    public Guid? SizeRunSizeId { get; set; }
    public ProductSizeRunSize? SizeRunSize { get; set; }

    public int UnitsPlanned { get; set; }
    public int UnitsProduced { get; set; }
    public int UnitsSurplus { get; set; }

    public string Disposition { get; set; } = "pending";
    // pending | assigned_to_order | to_inventory | scrapped

    public Guid? AssignedOrderId { get; set; }
    public ProductionOrder? AssignedOrder { get; set; }

    public Guid? WarehouseEntryId { get; set; }

    public string Notes { get; set; } = string.Empty;
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedBy { get; set; }
}
