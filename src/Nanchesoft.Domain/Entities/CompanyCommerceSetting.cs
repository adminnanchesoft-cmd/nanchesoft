using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class CompanyCommerceSetting : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public bool B2BEnabled { get; set; }
    public string? B2BLogoUrl { get; set; }
    public string? B2BDomain { get; set; }
    public string? B2BPrimaryColorHex { get; set; }
    public string? B2BWelcomeMessageMd { get; set; }

    public Guid? DefaultPriceListId { get; set; }
    public Guid? DefaultWarehouseId { get; set; }

    public decimal MinOrderAmount { get; set; }
    public bool AllowsBackorders { get; set; } = true;

    public string? ShippingPolicyMd { get; set; }
    public string? ReturnPolicyMd { get; set; }
    public string? PrivacyPolicyMd { get; set; }

    public string? CfdiUsoDefault { get; set; }
    public string? MetodoPagoDefault { get; set; }
    public string? FormaPagoDefault { get; set; }
}
