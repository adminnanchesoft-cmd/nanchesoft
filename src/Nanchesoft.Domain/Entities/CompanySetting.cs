using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class CompanySetting : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid CurrencyId { get; set; }
    public Currency? Currency { get; set; }

    public string Timezone { get; set; } = "America/Mexico_City";
    public int MonetaryDecimals { get; set; } = 2;
    public int QuantityDecimals { get; set; } = 2;

    public Guid? DefaultPurchaseSeriesId { get; set; }
    public DocumentSeries? DefaultPurchaseSeries { get; set; }

    public Guid? DefaultSalesSeriesId { get; set; }
    public DocumentSeries? DefaultSalesSeries { get; set; }
}
