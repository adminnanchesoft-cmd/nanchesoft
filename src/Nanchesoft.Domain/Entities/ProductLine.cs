using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class ProductLine : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? ProductFamilyId { get; set; }
    public ProductFamily? ProductFamily { get; set; }

    public Guid? ProductLastId { get; set; }
    public ProductLast? ProductLast { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public bool AllowsDiscount { get; set; }
}
