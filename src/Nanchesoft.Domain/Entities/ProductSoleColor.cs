using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class ProductSoleColor : BaseEntity, IOrangeSimpleCatalogEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Sequence { get; set; }
}
