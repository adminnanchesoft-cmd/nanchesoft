using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class PayrollSourceApplication : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid PayrollRunId { get; set; }
    public PayrollRun? PayrollRun { get; set; }

    public Guid? PayrollRunLineId { get; set; }
    public PayrollRunLine? PayrollRunLine { get; set; }

    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public Guid? PayrollPeriodId { get; set; }
    public PayrollPeriod? PayrollPeriod { get; set; }

    public Guid? PayrollConceptId { get; set; }
    public PayrollConcept? PayrollConcept { get; set; }

    public Guid? SourceId { get; set; }
    public string SourceType { get; set; } = "prepayroll_adjustment";
    public string ApplicationCode { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = string.Empty;
    public string MovementType { get; set; } = "perception";
    public decimal Quantity { get; set; } = 1m;
    public decimal Amount { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal ExemptAmount { get; set; }
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "applied";
    public string Notes { get; set; } = string.Empty;
}
