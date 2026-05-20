using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class B2BAccount : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string Status { get; set; } = "pendingApproval";

    public decimal CreditLine { get; set; }
    public decimal CreditUsed { get; set; }

    public Guid? DefaultPriceListId { get; set; }
    public ItemPriceList? DefaultPriceList { get; set; }

    public Guid? DefaultWarehouseId { get; set; }

    public string? PreferredCfdiUse { get; set; }
    public string? PreferredPaymentMethod { get; set; }
    public bool RequiresPo { get; set; }
    public bool AllowSelfCheckout { get; set; } = true;

    public string? Locale { get; set; }
    public string? Timezone { get; set; }
}
