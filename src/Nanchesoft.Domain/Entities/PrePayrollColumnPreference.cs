using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class PrePayrollColumnPreference : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? PayrollPeriodId { get; set; }
    public PayrollPeriod? PayrollPeriod { get; set; }

    public string UserKey { get; set; } = "default";
    public string ConceptIds { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
