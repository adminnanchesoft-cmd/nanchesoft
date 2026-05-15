using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class ItemPriceList : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? CurrencyId { get; set; }
    public Currency? Currency { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }

    public ICollection<ItemPriceListDetail> Details { get; set; } = new List<ItemPriceListDetail>();
}
