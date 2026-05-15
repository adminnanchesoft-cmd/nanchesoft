using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class ServiceCatalogItem : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BillingUnit { get; set; } = "HORA";
    public decimal DefaultRate { get; set; }
    public string? Notes { get; set; }
}
