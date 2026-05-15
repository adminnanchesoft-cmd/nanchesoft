using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class PayrollRunLineDetail : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid PayrollRunId { get; set; }
    public PayrollRun? PayrollRun { get; set; }

    public Guid PayrollRunLineId { get; set; }
    public PayrollRunLine? PayrollRunLine { get; set; }

    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public Guid PayrollConceptId { get; set; }
    public PayrollConcept? PayrollConcept { get; set; }

    public string ConceptCode { get; set; } = string.Empty;
    public string ConceptName { get; set; } = string.Empty;
    public string ConceptType { get; set; } = "perception";
    public string SatCode { get; set; } = string.Empty;
    public string TaxableType { get; set; } = "taxable";
    public decimal Quantity { get; set; } = 1m;
    public decimal Amount { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal ExemptAmount { get; set; }
    public int SortOrder { get; set; }
    public bool IsGenerated { get; set; } = true;
    public string Status { get; set; } = "active";
    public string Notes { get; set; } = string.Empty;
}
