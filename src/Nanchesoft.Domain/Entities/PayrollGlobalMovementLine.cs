using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class PayrollGlobalMovementLine : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid PayrollGlobalMovementId { get; set; }
    public PayrollGlobalMovement? PayrollGlobalMovement { get; set; }

    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public Guid? PayrollPeriodId { get; set; }
    public PayrollPeriod? PayrollPeriod { get; set; }

    public decimal Quantity { get; set; } = 1m;
    public decimal Amount { get; set; }

    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public string AppliedBy { get; set; } = string.Empty;

    public Guid? ResultingAdjustmentId { get; set; }
    public Guid? ResultingRecurringMovementId { get; set; }

    public string Status { get; set; } = "applied";
    public string Notes { get; set; } = string.Empty;
}
