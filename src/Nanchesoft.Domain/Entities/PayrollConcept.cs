using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class PayrollConcept : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ConceptType { get; set; } = string.Empty;
    public string CalculationType { get; set; } = string.Empty;
    public string SatCode { get; set; } = string.Empty;
    public string TaxableType { get; set; } = string.Empty;
    public bool IsRecurring { get; set; }
}
