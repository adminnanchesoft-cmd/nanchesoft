using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class ProductionScheduleLine : BaseEntity
{
    public Guid ProductionScheduleId { get; set; }
    public ProductionSchedule? ProductionSchedule { get; set; }

    public Guid ProductionOrderId { get; set; }
    public ProductionOrder? ProductionOrder { get; set; }

    public Guid ProductionOrderLineId { get; set; }
    public ProductionOrderLine? ProductionOrderLine { get; set; }

    public Guid? ProductionCellId { get; set; }
    public ProductionCell? ProductionCell { get; set; }

    public Guid ProductionPhaseId { get; set; }
    public ProductionPhase? ProductionPhase { get; set; }

    public DateOnly ScheduledDate { get; set; }
    public int UnitsScheduled { get; set; }
    public int UnitsProduced { get; set; }
    public string Shift { get; set; } = "morning";
    // morning | afternoon | night
    public string Notes { get; set; } = string.Empty;
}
