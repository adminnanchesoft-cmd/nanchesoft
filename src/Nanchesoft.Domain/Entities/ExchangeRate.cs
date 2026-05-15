using Nanchesoft.Domain.Common;

namespace Nanchesoft.Domain.Entities;

public sealed class ExchangeRate : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public Guid CurrencyId { get; set; }
    public Currency? Currency { get; set; }

    public DateTime RateDate { get; set; } = DateTime.UtcNow.Date;
    public decimal BuyRate { get; set; }
    public decimal SellRate { get; set; }
    public decimal ReferenceRate { get; set; }
}
