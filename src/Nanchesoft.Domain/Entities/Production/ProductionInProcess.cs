using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class ProductionInProcess : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid ProductionOrderId { get; set; }
    public ProductionOrder? ProductionOrder { get; set; }

    public Guid ProductionOrderLineId { get; set; }
    public ProductionOrderLine? ProductionOrderLine { get; set; }

    public Guid ProductionPhaseId { get; set; }
    public ProductionPhase? ProductionPhase { get; set; }

    public Guid? ProductionCellId { get; set; }
    public ProductionCell? ProductionCell { get; set; }

    public DateOnly EntryDate { get; set; }
    public int UnitsEntered { get; set; }
    public int UnitsExited { get; set; }
    public int UnitsRejected { get; set; }
    public int UnitsCurrent { get; set; }

    public string? EnteredBy { get; set; }
    public string Notes { get; set; } = string.Empty;
}
