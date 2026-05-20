using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class PayrollGlobalMovement : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid PayrollConceptId { get; set; }
    public PayrollConcept? PayrollConcept { get; set; }

    public string BatchCode { get; set; } = string.Empty;
    public string BatchName { get; set; } = string.Empty;
    public string MovementType { get; set; } = "perception";
    public string CalculationMode { get; set; } = "fixed";

    public decimal Quantity { get; set; } = 1m;
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public int TimesToApply { get; set; }
    public int TimesApplied { get; set; }

    public decimal MaxAmount { get; set; }
    public decimal AccumulatedAmount { get; set; }

    public string ControlNumber { get; set; } = string.Empty;

    public string FilterDepartmentIds { get; set; } = string.Empty;
    public string FilterPositionIds { get; set; } = string.Empty;
    public string FilterBranchIds { get; set; } = string.Empty;
    public string FilterEmployerRegistrationIds { get; set; } = string.Empty;
    public string FilterWorkShiftIds { get; set; } = string.Empty;
    public string FilterEmployeeIds { get; set; } = string.Empty;
    public string ExcludeEmployeeIds { get; set; } = string.Empty;
    public decimal MinSalary { get; set; }
    public decimal MaxSalary { get; set; }

    public bool MakeRecurring { get; set; }
    public string Status { get; set; } = "draft";

    public DateTime? AppliedAt { get; set; }
    public string AppliedBy { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;
}
