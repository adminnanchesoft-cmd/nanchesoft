using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class PayrollPeriodType : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int DaysPerPeriod { get; set; }
    public int PeriodsPerYear { get; set; }
    public string Notes { get; set; } = string.Empty;
}
