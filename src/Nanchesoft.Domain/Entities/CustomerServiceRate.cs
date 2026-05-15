using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class CustomerServiceRate : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid ServiceCatalogItemId { get; set; }
    public ServiceCatalogItem? ServiceCatalogItem { get; set; }

    public Guid? CurrencyId { get; set; }
    public Currency? Currency { get; set; }

    public decimal Rate { get; set; }
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow.Date;
    public DateTime? EffectiveTo { get; set; }
    public string? Notes { get; set; }
}
