using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class PrePayrollAdjustment : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public Guid PayrollPeriodId { get; set; }
    public PayrollPeriod? PayrollPeriod { get; set; }

    public Guid? PayrollConceptId { get; set; }
    public PayrollConcept? PayrollConcept { get; set; }

    public string AdjustmentCode { get; set; } = string.Empty;
    public string AdjustmentName { get; set; } = string.Empty;
    public string AdjustmentType { get; set; } = "perception";
    public string CaptureSource { get; set; } = "manual";
    public DateTime ReferenceDate { get; set; }
    public decimal Quantity { get; set; } = 1m;
    public decimal Amount { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal ExemptAmount { get; set; }
    public string Status { get; set; } = "captured";
    public string Notes { get; set; } = string.Empty;
}
