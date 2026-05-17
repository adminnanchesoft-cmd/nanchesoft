using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class ProductionSchedule : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid BranchId { get; set; }
    public Branch? Branch { get; set; }

    public string WeekCode { get; set; } = string.Empty;
    public DateOnly WeekStartDate { get; set; }
    public DateOnly WeekEndDate { get; set; }

    public string Status { get; set; } = "open";
    // open | planning | locked | closed

    public int TotalCapacityUnits { get; set; }
    public int TotalScheduledUnits { get; set; }
    public int TotalProducedUnits { get; set; }
    public decimal LoadPercentage { get; set; }
    public string Notes { get; set; } = string.Empty;

    public DateTime? LockedAt { get; set; }
    public string? LockedBy { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? ClosedBy { get; set; }

    public ICollection<ProductionScheduleLine> Lines { get; set; } = new List<ProductionScheduleLine>();
}
