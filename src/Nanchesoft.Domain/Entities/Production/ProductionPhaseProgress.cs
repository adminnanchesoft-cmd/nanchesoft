using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class ProductionPhaseProgress : BaseEntity
{
    public Guid ProductionOrderId { get; set; }
    public ProductionOrder? ProductionOrder { get; set; }

    public Guid ProductionOrderLineId { get; set; }
    public ProductionOrderLine? ProductionOrderLine { get; set; }

    public Guid ProductionPhaseId { get; set; }
    public ProductionPhase? ProductionPhase { get; set; }

    public int UnitsPlanned { get; set; }
    public int UnitsInProgress { get; set; }
    public int UnitsCompleted { get; set; }
    public int UnitsRejected { get; set; }
    public int UnitsPending { get; set; }

    public string Status { get; set; } = "pending";
    // pending | in_progress | completed | blocked

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    public int RescheduledCount { get; set; }
    public string? LastRescheduleReason { get; set; }
}
