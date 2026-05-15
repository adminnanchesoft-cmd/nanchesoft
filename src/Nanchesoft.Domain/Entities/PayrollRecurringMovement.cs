using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class PayrollRecurringMovement : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public Guid PayrollConceptId { get; set; }
    public PayrollConcept? PayrollConcept { get; set; }

    public string MovementCode { get; set; } = string.Empty;
    public string MovementName { get; set; } = string.Empty;
    public string MovementType { get; set; } = "perception";
    public string CalculationMode { get; set; } = "fixed";
    public decimal Quantity { get; set; } = 1m;
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
    public DateTime EffectiveStartDate { get; set; }
    public DateTime? EffectiveEndDate { get; set; }
    public bool ApplyEveryRun { get; set; } = true;
    public int? DayOfPeriod { get; set; }
    public bool IsProrated { get; set; }
    public string Status { get; set; } = "active";
    public string Notes { get; set; } = string.Empty;
}
