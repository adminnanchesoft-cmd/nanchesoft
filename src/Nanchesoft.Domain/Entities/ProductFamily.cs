using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class ProductFamily : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string StatisticsGroup { get; set; } = string.Empty;
    public bool IsFinishedProductFamily { get; set; } = true;
}
